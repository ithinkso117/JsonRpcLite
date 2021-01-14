using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    internal class JsonRpcWebsocketDispatcher : JsonRpcHttpDispatcher
    {

        /// <summary>
        /// Dispatch request to different services.
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The context for communication</param>
        /// <returns>Void</returns>
        public override async Task DispatchCall(JsonRpcService service, object context)
        {
            if (context is WebSocketContext webSocketContext)
            {
                await DispatchCall(service, webSocketContext);
            }
            else
            {
                throw new InvalidOperationException("The context is not a WebSocketContext");
            }
        }

        /// <summary>
        /// Dispatch request to different services.
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The http context</param>
        /// <returns>Void</returns>
        private async Task DispatchCall(JsonRpcService service, WebSocketContext context)
        {
            var webSocket = context.WebSocket;
            while (true)
            {
                if (webSocket.CloseStatus != null)
                {
                    throw new WebSocketException((int)webSocket.CloseStatus, webSocket.CloseStatusDescription);
                }

                JsonRpcRequest[] requests;
                await using (var requestStream = await GetRequestStreamAsync(webSocket).ConfigureAwait(false))
                {
                    var requestData = ArrayPool<byte>.Shared.Rent((int)requestStream.Length);
                    try
                    {
                        await ReadRequestDataAsync(requestStream, requestData).ConfigureAwait(false);
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
                }
            }
        }

        /// <summary>
        /// Get request stream from the websocket.
        /// </summary>
        /// <param name="webSocket">The websocket to handle.</param>
        /// <returns>The stream which contains the request.</returns>
        private async Task<Stream> GetRequestStreamAsync(WebSocket webSocket)
        {
            var stream = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(16384);
            try
            {
                while (true)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                    if (result.CloseStatus != null)
                    {
                        throw new WebSocketException((int)result.CloseStatus, result.CloseStatusDescription);
                    }
                    else
                    {
                        stream.Write(buffer, 0, result.Count);
                    }
                    if (result.EndOfMessage)
                    {
                        return stream;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Write rpc result struct data to remote side.
        /// </summary>
        /// <param name="context">The context of the http.</param>
        /// <param name="result">The result data to write</param>
        /// <returns>Void</returns>
        protected override async Task WriteResultAsync(object context, byte[] result = null)
        {
            if (context is WebSocketContext webSocketContext)
            {
                //Empty value will not give anything back.
                if (result != null)
                {
                    try
                    {
                        var webSocket = webSocketContext.WebSocket;
                        await webSocket.SendAsync(new ArraySegment<byte>(result), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteWarning($"Write result back to client error:{ex}");
                    }
                }
            }
            else
            {
                Logger.WriteWarning("Write result back to client error: The context is not a WebSocketContext");
            }

        }
    }
}
