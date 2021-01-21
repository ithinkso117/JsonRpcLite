using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    internal class JsonRpcHttpDispatcher:IJsonRpcDispatcher
    {

        /// <summary>
        /// Dispatch request to different services.
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The context for communication</param>
        /// <returns>Void</returns>
        public virtual async Task DispatchCall(JsonRpcService service, object context)
        {
            if (context is HttpListenerContext httpListenerContext)
            {
                await DispatchCall(service, httpListenerContext);
            }
            else
            {
                throw new InvalidOperationException("The context is not a HttpListenerContext");
            }
        }


        /// <summary>
        /// Handle request request and get the response
        /// </summary>
        /// <param name="service">The service which will handle the request.</param>
        /// <param name="request">The request to handle.</param>
        /// <returns>The response for the request.</returns>
        protected async Task<JsonRpcResponse> GetResponseAsync(JsonRpcService service, JsonRpcRequest request)
        {
            JsonRpcResponse response = null;
            try
            {
                var rpcCall = service.GetRpcCall(request.Method);
                if (rpcCall == null)
                {
                    throw new MethodNotFoundException($"Method: {request.Method} not found.");
                }
                var arguments = await JsonRpcCodec.DecodeArgumentsAsync(request.Params, rpcCall.Parameters).ConfigureAwait(false);

                //From here we got the response id.
                response = new JsonRpcResponse(request.Id);
                //The parser will add context into the args, so the final count is parameter count + 1.
                if (arguments.Length == rpcCall.Parameters.Count)
                {
                    try
                    {
                        var result = await rpcCall.Call(arguments).ConfigureAwait(false);
                        if (request.IsNotification)
                        {
                            return null;
                        }
                        response.WriteResult(result);
                    }
                    catch (Exception ex)
                    {
                        var argumentString = new StringBuilder();
                        argumentString.Append(Environment.NewLine);
                        var index = 0;
                        foreach (var argument in arguments)
                        {
                            argumentString.AppendLine($"[{index}] {argument}");
                        }

                        argumentString.Append(Environment.NewLine);
                        Logger.WriteError($"Call method {rpcCall.Name} with args:{argumentString} error :{ex.Format()}");
                        response.WriteResult(new InternalErrorException());
                    }
                }
                else
                {
                    throw new InvalidParamsException("Argument count is not matched");
                }
            }
            catch (Exception ex)
            {
                response ??= new JsonRpcResponse();
                if (ex is RpcException rpcException)
                {
                    Logger.WriteError($"Handle request {request} error: {rpcException.Format()}");
                    response.WriteResult(rpcException);
                }
                else
                {
                    Logger.WriteError($"Handle request {request} error: {ex.Format()}");
                    var serverError = new InternalErrorException();
                    response.WriteResult(serverError);
                }

            }
            return response;
        }

        /// <summary>
        /// Read the request data from the input stream.
        /// </summary>
        /// <param name="input">The stream to handle.</param>
        /// <param name="requestData">The request data to fill.</param>
        protected async Task ReadRequestDataAsync(Stream input, byte[] requestData)
        {
            //TODO add limitation plugin etc.
            var length = requestData.Length;
            var offset = 0;
            while (length > 0)
            {
                var readLength = await input.ReadAsync(requestData, offset, length).ConfigureAwait(false);
                length -= readLength;
                offset += readLength;
            }
        }

        /// <summary>
        /// Dispatch request to different services.
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The http context</param>
        /// <returns>Void</returns>
        private async Task DispatchCall(JsonRpcService service, HttpListenerContext context)
        {
            var httpMethod = context.Request.HttpMethod.ToLower();
            if (httpMethod != "post")
            {
                throw new ServerErrorException("Invalid http-method.", $"Invalid http-method:{httpMethod}");
            }

            Logger.WriteVerbose($"Handle request [{httpMethod}]: {context.Request.Url}");


            var requestData = ArrayPool<byte>.Shared.Rent((int)context.Request.ContentLength64);
            JsonRpcRequest[] requests;
            try
            {
                await ReadRequestDataAsync(context.Request.InputStream, requestData).ConfigureAwait(false);
                context.Request.InputStream.Close();
                if (Logger.DebugMode)
                {
                    var requestString = Encoding.UTF8.GetString(requestData);
                    Logger.WriteDebug($"Receive request data:{requestString}");
                }
                requests = await JsonRpcCodec.DecodeRequestsAsync(requestData).ConfigureAwait(false);

            }
            finally
            {
                ArrayPool<byte>.Shared.Return(requestData);
            }

            if (requests.Length == 1)
            {
                var request = requests[0];
                var response = await GetResponseAsync(service, request).ConfigureAwait(false);
                await WriteResponse(context, response).ConfigureAwait(false);

            }
            else
            {
                //batch call.
                var responseList = new List<JsonRpcResponse>();
                foreach (var request in requests)
                {
                    var response = await GetResponseAsync(service, request).ConfigureAwait(false);
                    if (response != null)
                    {
                        responseList.Add(response);
                    }
                }

                if (responseList.Count > 0)
                {
                    await WriteResponses(context, responseList.ToArray()).ConfigureAwait(false);
                }
                else
                {
                    await WriteResultAsync(context).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Write a JsonRpcHttpResponse to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="response">The JsonRpcHttpResponse to write.</param>
        /// <returns>Void</returns>
        public async Task WriteResponse(object context, JsonRpcResponse response)
        {
            var resultData = await JsonRpcCodec.EncodeResponsesAsync(new[] {response}).ConfigureAwait(false);
            await WriteResultAsync(context, resultData).ConfigureAwait(false);
        }

        /// <summary>
        /// Write a group of JsonRpcHttpResponse to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="responses">A group of JsonRpcHttpResponses to write.</param>
        /// <returns>Void</returns>
        public async Task WriteResponses(object context, JsonRpcResponse[] responses)
        {
            var resultData = await JsonRpcCodec.EncodeResponsesAsync(responses).ConfigureAwait(false);
            await WriteResultAsync(context, resultData).ConfigureAwait(false);
        }


        /// <summary>
        /// Write rpc result struct data to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="result">The result data to write</param>
        /// <returns>Void</returns>
        protected virtual async Task WriteResultAsync(object context, byte[] result = null)
        {
            if (context is HttpListenerContext httpHttpListenerContext)
            {
                try
                {
                    httpHttpListenerContext.Response.AddHeader("Server", "JsonRpcLite");
                    httpHttpListenerContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    httpHttpListenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
                    httpHttpListenerContext.Response.ContentEncoding = Encoding.UTF8;
                    httpHttpListenerContext.Response.ContentType = "application/json";
                    if (result != null)
                    {
                        httpHttpListenerContext.Response.ContentLength64 = result.Length;
                        await httpHttpListenerContext.Response.OutputStream.WriteAsync(result).ConfigureAwait(false);
                    }

                    httpHttpListenerContext.Response.Close();
                    if (Logger.DebugMode)
                    {
                        if (result != null)
                        {
                            var resultString = Encoding.UTF8.GetString(result);
                            Logger.WriteDebug($"Response data sent:{resultString}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning($"Write result back to client error:{ex}");
                }
            }
            else
            {
                Logger.WriteWarning("Write result back to client error: The context is not a HttpListenerContext");
            }
        }

    }
}
