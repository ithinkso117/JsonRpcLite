using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Rpc;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public abstract class JsonRpcHttpServerEngineBase :IJsonRpcServerEngine
    {
        /// <summary>
        /// Gets whether the smd function is enabled.
        /// </summary>
        protected abstract bool SmdEnabled { get; }

        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public abstract void Start(IJsonRpcRouter router);

        /// <summary>
        /// Stop the engine.
        /// </summary>
        public abstract void Stop();


        /// <summary>
        /// Read the request data from the input stream.
        /// </summary>
        /// <param name="inputStream">The stream to handle.</param>
        /// <param name="requestData">The request data to fill.</param>
        /// <param name="dataLength">The data length to read.</param>
        private async Task ReadRequestDataAsync(Stream inputStream, byte[] requestData, int dataLength)
        {
            var length = dataLength;
            var offset = 0;
            while (length > 0)
            {
                var readLength = await inputStream.ReadAsync(requestData, offset, length).ConfigureAwait(false);
                length -= readLength;
                offset += readLength;
            }
        }


        /// <summary>
        /// Dispatch request to specified service.
        /// </summary>
        /// <param name="context">The HttpListenerContext</param>
        /// <param name="router">The router to handle the rpc request</param>
        /// <param name="serviceName">The name of the service</param>
        /// <returns>Void</returns>
        private async Task DispatchAsync(IJsonRpcHttpContext context, IJsonRpcRouter router, string serviceName)
        {
            var dataLength = (int)context.GetRequestContentLength();
            var requestData = ArrayPool<byte>.Shared.Rent(dataLength);
            JsonRpcRequest[] requests;
            try
            {
                var inputStream = context.GetInputStream();
                await ReadRequestDataAsync(inputStream, requestData, dataLength).ConfigureAwait(false);
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

            var responses = await router.DispatchRequestsAsync(serviceName, requests).ConfigureAwait(false);
            await WriteRpcResponsesAsync(context, responses).ConfigureAwait(false);
        }


        /// <summary>
        /// Parser the request url, get the calling information.
        /// </summary>
        /// <param name="requestPath">The path requested by the caller.</param>
        /// <returns>The service name parsed from the uri.</returns>
        private string GetRpcServiceName(string requestPath)
        {
            var url = $"{requestPath.Trim('/')}";
            var urlParts = url.Split('/');
            if (urlParts.Length != 1) return null;
            return urlParts[0];
        }



        /// <summary>
        /// Write smd data back to the client.
        /// </summary>
        /// <param name="context">The http context</param>
        /// <param name="smdData">The smd data to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteSmdDataAsync(IJsonRpcHttpContext context, byte[] smdData)
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
        /// <param name="context">The http context</param>
        /// <param name="exception">The exception to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteHttpExceptionAsync(IJsonRpcHttpContext context, HttpException exception)
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
        /// <param name="context">The http context</param>
        /// <param name="responses">The responses to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteRpcResponsesAsync(IJsonRpcHttpContext context, JsonRpcResponse[] responses)
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
        /// <param name="context">The http context.</param>
        /// <param name="data">The data to output.</param>
        /// <returns>The compressed or not output data.</returns>
        private async Task<byte[]> GetOutputDataAsync(IJsonRpcHttpContext context, byte[] data)
        {
            var outputData = data;
            var acceptEncoding = context.GetRequestHeader("Accept-Encoding");
            if (acceptEncoding != null && acceptEncoding.Contains("gzip"))
            {
                context.SetResponseHeader("Content-Encoding", "gzip");
                await using var memoryStream = new MemoryStream();
                await using var outputStream = new GZipStream(memoryStream, CompressionMode.Compress);
                await outputStream.WriteAsync(outputData).ConfigureAwait(false);
                await outputStream.FlushAsync().ConfigureAwait(false);
                outputData = memoryStream.ToArray();
            }
            else if (acceptEncoding != null && acceptEncoding.Contains("deflate"))
            {
                context.SetResponseHeader("Content-Encoding", "deflate");
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
        /// <param name="context">The  http context.</param>
        /// <param name="statusCode">The status code to return</param>
        /// <param name="message">The message to write back.</param>
        /// <returns>Void</returns>
        private async Task WriteHttpResultAsync(IJsonRpcHttpContext context, int statusCode, string message)
        {
            context.SetResponseHeader("Server", "JsonRpcLite");
            context.SetResponseHeader("Access-Control-Allow-Origin", "*");
            context.SetResponseStatusCode(statusCode);
            context.SetResponseContentType("text/html");
            var outputData = await GetOutputDataAsync(context, Encoding.UTF8.GetBytes(message)).ConfigureAwait(false);
            context.SetResponseContentLength(outputData.Length);
            var outputStream = context.GetOutputStream();
            await outputStream.WriteAsync(outputData).ConfigureAwait(false);
            await outputStream.FlushAsync();
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
        private async Task WriteRpcResultAsync(IJsonRpcHttpContext context, byte[] result = null)
        {
            context.SetResponseHeader("Server", "JsonRpcLite");
            context.SetResponseHeader("Access-Control-Allow-Origin", "*");
            context.SetResponseStatusCode( (int)HttpStatusCode.OK);
            context.SetResponseContentType("application/json");
            if (result != null)
            {
                var outputData = await GetOutputDataAsync(context, result).ConfigureAwait(false);
                context.SetResponseContentLength(outputData.Length);
                var outputStream = context.GetOutputStream();
                await outputStream.WriteAsync(outputData).ConfigureAwait(false);
                await outputStream.FlushAsync();
            }
            if (Logger.DebugMode)
            {
                if (result != null)
                {
                    var resultString = Encoding.UTF8.GetString(result);
                    Logger.WriteDebug($"Response data sent:{resultString}");
                }
            }
        }


        /// <summary>
        /// Handle connected request and return result.
        /// </summary>
        /// <param name="context">The http context to handle.</param>
        /// <param name="router">The router to dispatch the request data.</param>
        /// <returns>Void</returns>
        protected async Task HandleContextAsync(IJsonRpcHttpContext context, IJsonRpcRouter router)
        {
            var httpMethod = context.GetRequestHttpMethod();
            var requestPath = context.GetRequestPath();
            Logger.WriteVerbose($"Handle request [{httpMethod}]: {requestPath}");
            try
            {
                var serviceName = GetRpcServiceName(requestPath);
                if (string.IsNullOrEmpty(serviceName))
                {
                    Logger.WriteWarning($"Service for request: {requestPath} not found.");
                    throw new HttpException((int)HttpStatusCode.ServiceUnavailable, "Service does not exist.");
                }

                if (httpMethod == "get")
                {
                    var smdRequest = false;
                    var smdIndex = serviceName.LastIndexOf(".smd", StringComparison.InvariantCultureIgnoreCase);
                    if (smdIndex != -1)
                    {
                        serviceName = serviceName.Substring(0, smdIndex);
                        smdRequest = true;
                    }

                    if (!router.ServiceExists(serviceName))
                    {
                        Logger.WriteWarning($"Service for request: {requestPath} not found.");
                        throw new HttpException((int)HttpStatusCode.ServiceUnavailable, $"Service [{serviceName}] does not exist.");
                    }

                    if (SmdEnabled && smdRequest)
                    {
                        try
                        {
                            var smdData = await router.GetServiceSmdData(serviceName);
                            await WriteSmdDataAsync(context, smdData).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            throw new HttpException((int)HttpStatusCode.InternalServerError, ex.Message);
                        }
                    }
                    else
                    {
                        throw new HttpException((int)HttpStatusCode.NotFound, $"Resource for {requestPath} does not exist.");
                    }
                }
                else if (httpMethod == "post")
                {
                    if (!router.ServiceExists(serviceName))
                    {
                        Logger.WriteWarning($"Service for request: {requestPath} not found.");
                        throw new ServerErrorException("Service does not exist.", $"Service [{serviceName}] does not exist.");
                    }
                    try
                    {
                        await DispatchAsync(context, router, serviceName).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex is RpcException)
                        {
                            throw;
                        }
                        throw new ServerErrorException("Internal server error.", ex.Message);
                    }

                }
                else
                {
                    throw new HttpException((int)HttpStatusCode.MethodNotAllowed, $"Invalid http-method:{httpMethod}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Handle request {requestPath} error: {ex.Message}");
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
                        var serverError = new InternalErrorException($"Handle request {requestPath} error: {ex.Message}");
                        response.WriteResult(serverError);
                    }
                    await WriteRpcResponsesAsync(context, new[] { response }).ConfigureAwait(false);
                }

            }
        }
    }
}
