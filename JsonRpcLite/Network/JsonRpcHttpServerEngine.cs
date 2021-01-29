using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Rpc;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public class JsonRpcHttpServerEngine : IJsonRpcServerEngine
    {
        private readonly ManualResetEvent _stopEvent = new(true);
        private readonly string _prefix;
        private readonly bool _enableSmd;

        private HttpListener _listener;


        private IJsonRpcRouter _router;

        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; protected set; }


        public JsonRpcHttpServerEngine(string prefix, bool enableSmd = true)
        {
            Name = nameof(JsonRpcHttpServerEngine);
            _prefix = prefix;
            _enableSmd = enableSmd;
        }

        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public void Start(IJsonRpcRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _stopEvent.Reset();
            _listener = new HttpListener();
            _listener.Prefixes.Add(_prefix);
            _listener.Start();
            Task.Factory.StartNew(async () =>
            {
                while (!_stopEvent.WaitOne(1))
                {
                    try
                    {
                        var context = await _listener.GetContextAsync().ConfigureAwait(false);
                        HandleContextAsync(context);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError($"GetContext error:{ex.Format()}");
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Logger.WriteInfo("JsonRpc http server engine started.");
        }


        /// <summary>
        /// Stop the engine.
        /// </summary>
        public void Stop()
        {
            _router = null;
            _stopEvent.Set();
            _listener.Close();
            Logger.WriteInfo("JsonRpc http server engine stopped.");
        }

        /// <summary>
        /// Read the request data from the input stream.
        /// </summary>
        /// <param name="input">The stream to handle.</param>
        /// <param name="requestData">The request data to fill.</param>
        /// <param name="dataLength">The data length to read.</param>
        private async Task ReadRequestDataAsync(Stream input, byte[] requestData, int dataLength)
        {
            var length = dataLength;
            var offset = 0;
            while (length > 0)
            {
                var readLength = await input.ReadAsync(requestData, offset, length).ConfigureAwait(false);
                length -= readLength;
                offset += readLength;
            }
        }

        /// <summary>
        /// Parser the request url, get the calling information.
        /// </summary>
        /// <param name="requestUri">The uri requested by the caller.</param>
        /// <returns>The JsonRpcServiceInfo parsed from the uri.</returns>
        private JsonRpcServiceInfo GetRpcServiceInfo(Uri requestUri)
        {
            var url = $"{requestUri.AbsolutePath.Trim('/')}";
            var urlParts = url.Split('/');
            if (urlParts.Length != 1) return null;
            var serviceName = urlParts[0];
            return new JsonRpcServiceInfo(serviceName);
        }


        private async void HandleContextAsync(HttpListenerContext context)
        {
            try
            {
                var httpMethod = context.Request.HttpMethod.ToLower();
                Logger.WriteVerbose($"Handle request [{httpMethod}]: {context.Request.Url}");

                var serviceInfo = GetRpcServiceInfo(context.Request.Url);
                if (serviceInfo == null)
                {
                    Logger.WriteWarning($"Service for request: {context.Request.Url} not found.");
                    throw new HttpException((int)HttpStatusCode.ServiceUnavailable, "Service does not exist.");
                }

                if (httpMethod == "get")
                {
                    var smdRequest = false;
                    var serviceName = serviceInfo.Name;
                    var smdIndex = serviceName.LastIndexOf(".smd", StringComparison.InvariantCultureIgnoreCase);
                    if (smdIndex != -1)
                    {
                        serviceName = serviceName.Substring(0, smdIndex);
                        smdRequest = true;
                    }

                    if (!_router.ServiceExists(serviceName))
                    {
                        Logger.WriteWarning($"Service for request: {context.Request.Url} not found.");
                        throw new HttpException((int)HttpStatusCode.ServiceUnavailable,$"Service [{serviceName}] does not exist.");
                    }

                    if (_enableSmd && smdRequest)
                    {
                        try
                        {
                            var smdData = await _router.GetServiceSmdData(serviceName);
                            await WriteSmdDataAsync(context, smdData).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw new HttpException((int) HttpStatusCode.InternalServerError, ex.Message);
                        }
                    }
                    else
                    {
                        throw new HttpException((int)HttpStatusCode.NotFound,$"Resource for {context.Request.Url} does not exist.");
                    }
                }
                else if (httpMethod == "post")
                {
                    var serviceName = serviceInfo.Name;
                    if (!_router.ServiceExists(serviceName))
                    {
                        Logger.WriteWarning($"Service for request: {context.Request.Url} not found.");
                        throw new ServerErrorException("Service does not exist.", $"Service [{serviceName}] does not exist.");
                    }
                    try
                    {
                        await DispatchAsync(context, serviceName).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is RpcException)
                        {
                            throw;
                        }
                        throw new ServerErrorException("Internal server error.",ex.Message);
                    }

                }
                else
                {
                    throw new HttpException((int)HttpStatusCode.MethodNotAllowed,$"Invalid http-method:{httpMethod}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Handle request {context.Request.Url} error: {ex.Format()}");
                if (ex is HttpException httpException)
                {
                    await WriteHttpExceptionAsync(context, httpException).ConfigureAwait(false);
                }
                else
                {
                    var response = new JsonRpcResponse();
                    if (ex is RpcException rpcException)
                    {
                        response.WriteResult(rpcException);
                    }
                    else
                    {
                        var serverError = new InternalErrorException($"Handle request {context.Request.Url} error: {ex.Format()}");
                        response.WriteResult(serverError);
                    }
                    await WriteRpcResponsesAsync(context, new[] { response }).ConfigureAwait(false);
                }

            }
        }



        /// <summary>
        /// Dispatch request to specified service.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="serviceName">The name of the service</param>
        /// <returns>Void</returns>
        private async Task DispatchAsync(HttpListenerContext context, string serviceName)
        {
            var dataLength = (int) context.Request.ContentLength64;
            var requestData = ArrayPool<byte>.Shared.Rent(dataLength);
            JsonRpcRequest[] requests;
            try
            {
                await ReadRequestDataAsync(context.Request.InputStream, requestData, dataLength).ConfigureAwait(false);
                if (Logger.DebugMode)
                {
                    var requestString = Encoding.UTF8.GetString(requestData);
                    Logger.WriteDebug($"Receive request data:{requestString}");
                }

                requests = await JsonRpcCodec.DecodeRequestsAsync(requestData, dataLength).ConfigureAwait(false);

            }
            finally
            {
                ArrayPool<byte>.Shared.Return(requestData);
            }

            var responses = await _router.DispatchRequestsAsync(serviceName, requests).ConfigureAwait(false);
            await WriteRpcResponsesAsync(context, responses).ConfigureAwait(false);
        }


        /// <summary>
        /// Write smd data back to the client.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="smdData">The smd data to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteSmdDataAsync(HttpListenerContext context, byte[] smdData)
        {
            try
            {
                await WriteRpcResultAsync(context, smdData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteWarning($"Write smd data back to client error:{ex}");
            }
        }


        /// <summary>
        /// Write http exception back to the client.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="exception">The exception to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteHttpExceptionAsync(HttpListenerContext context, HttpException exception)
        {
            try
            {
                await WriteHttpResultAsync(context, exception.ErrorCode, exception.Message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteWarning($"Write http exception back to client error:{ex}");
            }
        }


        /// <summary>
        /// Write rpc responses back to the client.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="responses">The responses to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteRpcResponsesAsync(HttpListenerContext context, JsonRpcResponse[] responses)
        {
            try
            {
                var resultData = await JsonRpcCodec.EncodeResponsesAsync(responses).ConfigureAwait(false);
                await WriteRpcResultAsync(context, resultData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteWarning($"Write rpc response back to client error:{ex}");
            }
        }


        /// <summary>
        /// Get the compressed(or not) output data according to request.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="data">The data to output.</param>
        /// <returns>The compressed or not output data.</returns>
        private async Task<byte[]> GetOutputDataAsync(HttpListenerContext context, byte[] data)
        {
            var outputData = data;
            var acceptEncoding = context.Request.Headers["Accept-Encoding"];
            if (acceptEncoding != null && acceptEncoding.Contains("gzip"))
            {
                context.Response.AddHeader("Content-Encoding", "gzip");
                await using var memoryStream = new MemoryStream();
                await using var outputStream = new GZipStream(memoryStream, CompressionMode.Compress);
                await outputStream.WriteAsync(outputData).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
                outputData = memoryStream.ToArray();
            }
            else if (acceptEncoding != null && acceptEncoding.Contains("deflate"))
            {
                context.Response.AddHeader("Content-Encoding", "deflate");
                await using var memoryStream = new MemoryStream();
                await using var outputStream = new DeflateStream(memoryStream, CompressionMode.Compress);
                await outputStream.WriteAsync(outputData).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
                outputData = memoryStream.ToArray();
            }
            return outputData;
        }


        /// <summary>
        /// Write http message to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="statusCode">The status code to return</param>
        /// <param name="message">The message to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteHttpResultAsync(HttpListenerContext context, int statusCode, string message)
        {
            context.Response.AddHeader("Server", "JsonRpcLite");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.StatusCode = statusCode;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "text/html";
            var outputData = await GetOutputDataAsync(context, Encoding.UTF8.GetBytes(message)).ConfigureAwait(false);
            context.Response.ContentLength64 = outputData.Length;
            await context.Response.OutputStream.WriteAsync(outputData).ConfigureAwait(false);
            context.Response.Close();
            if (Logger.DebugMode)
            {
                Logger.WriteDebug($"Response data sent:{message}");
            }
        }


        /// <summary>
        /// Write rpc result struct data to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="result">The result data to write</param>
        /// <returns>Void</returns>
        private async Task WriteRpcResultAsync(HttpListenerContext context, byte[] result = null)
        {
            context.Response.AddHeader("Server", "JsonRpcLite");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "application/json";
            if (result != null)
            {
                var outputData = await GetOutputDataAsync(context, result).ConfigureAwait(false);
                context.Response.ContentLength64 = outputData.Length;
                await context.Response.OutputStream.WriteAsync(outputData).ConfigureAwait(false);
            }

            context.Response.Close();
            if (Logger.DebugMode)
            {
                if (result != null)
                {
                    var resultString = Encoding.UTF8.GetString(result);
                    Logger.WriteDebug($"Response data sent:{resultString}");
                }
            }

        }
    }
}
