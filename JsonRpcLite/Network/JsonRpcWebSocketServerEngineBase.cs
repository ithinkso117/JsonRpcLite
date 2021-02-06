using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Rpc;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public abstract class JsonRpcWebSocketServerEngineBase : IJsonRpcServerEngine
    {

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
        /// Handle data from websocket and return result data to remote client.
        /// </summary>
        /// <param name="requestPath">The request path from the http request.</param>
        /// <param name="router">The router to handle the request data.</param>
        /// <param name="socket">The connected websocket.</param>
        protected async Task HandleRequestAsync(string requestPath, IJsonRpcRouter router, WebSocket socket)
        {
            try
            {
                var serviceName = GetRpcServiceName(requestPath);
                if (string.IsNullOrEmpty(serviceName) || !router.ServiceExists(serviceName))
                {
                    Logger.WriteWarning($"Service {serviceName} does not exist.");
                    throw new InvalidOperationException($"Service [{serviceName}] does not exist.");
                }

                byte[] receiveBuffer = null;
                MemoryStream inputStream = null;
                // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
                while (socket.State == WebSocketState.Open)
                {
                    receiveBuffer ??= ArrayPool<byte>.Shared.Rent(128);
                    inputStream ??= new MemoryStream();
                    var receiveResult = await socket.ReceiveAsync(receiveBuffer, CancellationToken.None).ConfigureAwait(false);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Cannot accept text frame", CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        await inputStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
                        //handle stream
                        if (receiveResult.EndOfMessage)
                        {
                            //Release resources.
                            var requestData = inputStream.ToArray();
                            await inputStream.DisposeAsync().ConfigureAwait(false);
                            inputStream = null;
                            ArrayPool<byte>.Shared.Return(receiveBuffer);
                            receiveBuffer = null;

                            if (Logger.DebugMode)
                            {
                                var requestString = Encoding.UTF8.GetString(requestData);
                                Logger.WriteDebug($"Receive request data: {requestString}");
                            }

                            var requests = await JsonRpcCodec.DecodeRequestsAsync(requestData).ConfigureAwait(false);
                            var responses = await router.DispatchRequestsAsync(serviceName, requests).ConfigureAwait(false);
                            var responseData = await JsonRpcCodec.EncodeResponsesAsync(responses).ConfigureAwait(false);
                            await socket.SendAsync(responseData, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);

                            if (Logger.DebugMode)
                            {
                                var resultString = Encoding.UTF8.GetString(responseData);
                                Logger.WriteDebug($"Response data sent:{resultString}");
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Handle request {requestPath} error: {ex.Message}");
            }
            finally
            {
                socket.Dispose();
                Logger.WriteVerbose("Remote websocket closed.");
            }
        }


        /// <summary>
        /// Process data from websocket and return result data to remote client.
        /// </summary>
        /// <param name="requestPath">The request path from the http request.</param>
        /// <param name="router">The router to handle the request data.</param>
        /// <param name="socket">The connected websocket.</param>
        protected async void ProcessRequestAsync(string requestPath, IJsonRpcRouter router, WebSocket socket)
        {
            await HandleRequestAsync(requestPath, router, socket).ConfigureAwait(false);
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
    }
}
