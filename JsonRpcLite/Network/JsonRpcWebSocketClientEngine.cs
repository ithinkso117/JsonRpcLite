using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Rpc;

namespace JsonRpcLite.Network
{
    public class JsonRpcWebSocketClientEngine:IJsonRpcClientEngine
    {
        private class ClientWebSocketContext
        {
            /// <summary>
            /// Gets the waiter for the socket.
            /// </summary>
            public SemaphoreSlim Waiter { get; }

            public WebSocket Socket { get; }

            public ClientWebSocketContext(WebSocket socket)
            {
                Waiter = new SemaphoreSlim(1,1);
                Socket = socket;
            }
        }

        private readonly string _serverUrl;

        private readonly ConcurrentDictionary<string, ClientWebSocketContext> _contexts = new();


        /// <summary>
        /// Gets or sets the timeout of the engine.
        /// </summary>
        public int Timeout { get; set; }

        public string Name { get; }

        public JsonRpcWebSocketClientEngine(string serverUrl)
        {
            Name = nameof(JsonRpcWebSocketClientEngine);
            _serverUrl = serverUrl;
            Timeout = System.Threading.Timeout.Infinite;
        }

        private async Task<ClientWebSocketContext> ConnectAsync(string url)
        {
            var context = _contexts.GetOrAdd(url, key =>
            {
                var socket = CreateWebSocket(url).Result;
                return new ClientWebSocketContext(socket);
            });
            return await Task.FromResult(context);
        }

        private async Task<ClientWebSocket> CreateWebSocket(string url)
        {
            var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.AddSubProtocol("JsonRpcLite");
            await clientWebSocket.ConnectAsync(new Uri(url), CancellationToken.None).ConfigureAwait(false);
            return clientWebSocket;
        }

        public async Task<string> ProcessAsync(string serviceName, string requestString)
        {
            var requestData = Encoding.UTF8.GetBytes(requestString);
            var responseData = await ProcessAsync(serviceName, requestData).ConfigureAwait(false);
            return Encoding.UTF8.GetString(responseData);
        }

        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData)
        {
            var url = _serverUrl + serviceName;
            var context = await ConnectAsync(url).ConfigureAwait(false);
            await context.Waiter.WaitAsync();
            if (context.Socket.State == WebSocketState.Open)
            {
                await context.Socket.SendAsync(requestData, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024);
            var inputStream = new MemoryStream();
            try
            {
                while (context.Socket.State == WebSocketState.Open)
                {
                    var receiveResult = await context.Socket.ReceiveAsync(receiveBuffer, CancellationToken.None);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await context.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await context.Socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType,
                            "Cannot accept text frame", CancellationToken.None);
                    }
                    else
                    {
                        await inputStream.WriteAsync(receiveBuffer, 0, receiveResult.Count);
                        if (receiveResult.EndOfMessage)
                        {
                            return inputStream.ToArray();
                        }
                    }
                }
            }
            finally
            {
                await inputStream.DisposeAsync();
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                context.Waiter.Release();
            }

            return null;
        }
    }
}
