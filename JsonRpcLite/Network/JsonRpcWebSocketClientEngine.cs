using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Rpc;

namespace JsonRpcLite.Network
{
    public class JsonRpcWebSocketClientEngine:IJsonRpcClientEngine
    {
        private class ClientWebSocketContext:IDisposable
        {
            private bool _disposed;

            /// <summary>
            /// Gets the waiter for the socket.
            /// </summary>
            public SemaphoreSlim Waiter { get; }

            /// <summary>
            /// Gets the websocket of this context.
            /// </summary>
            public WebSocket Socket { get; }

            public ClientWebSocketContext(WebSocket socket)
            {
                Waiter = new SemaphoreSlim(1,1);
                Socket = socket;
            }

            ~ClientWebSocketContext()
            {
                Dispose();
            }


            public void Dispose()
            {
                if (!_disposed)
                {
                    Waiter?.Dispose();
                    Socket?.Dispose();
                    _disposed = true;
                    GC.SuppressFinalize(this);
                }
            }
        }

        private readonly string _serverUrl;
        private readonly bool _autoReconnect;

        private readonly ConcurrentDictionary<string, ClientWebSocketContext> _contexts = new();


        /// <summary>
        /// Gets or sets the timeout of the engine.
        /// </summary>
        public int Timeout { get; set; }


        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; }



        public JsonRpcWebSocketClientEngine(string serverUrl, bool autoReconnect = true)
        {
            Name = nameof(JsonRpcWebSocketClientEngine);
            _serverUrl = serverUrl;
            _autoReconnect = autoReconnect;
            Timeout = System.Threading.Timeout.Infinite;
        }

        /// <summary>
        /// Connect to the server and get the ClientWebSocketContext.
        /// </summary>
        /// <param name="url">The url to connect.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The ClientWebSocketContext</returns>
        private async Task<ClientWebSocketContext> ConnectAsync(string url, CancellationToken cancellationToken = default)
        {
            var context = _contexts.GetOrAdd(url, key =>
            {
                var socket = CreateWebSocket(key, cancellationToken).Result;
                return new ClientWebSocketContext(socket);
            });
            return await Task.FromResult(context).ConfigureAwait(false);
        }


        /// <summary>
        /// Create one websocket and connect to the server.
        /// </summary>
        /// <param name="url">The url to connect.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The created ClientWebSocket</returns>
        private async Task<ClientWebSocket> CreateWebSocket(string url, CancellationToken cancellationToken = default)
        {
            var clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.AddSubProtocol("JsonRpcLite");
            await clientWebSocket.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            return clientWebSocket;
        }


        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response string.</returns>
        public async Task<string> ProcessAsync(string serviceName, string requestString, CancellationToken cancellationToken = default)
        {
            var requestData = Encoding.UTF8.GetBytes(requestString);
            var responseData = await ProcessAsync(serviceName, requestData, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(responseData);
        }

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData, CancellationToken cancellationToken = default)
        {
            var url = _serverUrl + serviceName;
            var context = await ConnectAsync(url, cancellationToken).ConfigureAwait(false);
            await context.Waiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (context.Socket.State == WebSocketState.Open)
            {
                await context.Socket.SendAsync(requestData, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
            var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024);
            var inputStream = new MemoryStream();
            try
            {
                while (context.Socket.State == WebSocketState.Open)
                {
                    var receiveResult = await context.Socket.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await context.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        await context.Socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType,"Cannot accept text frame", cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await inputStream.WriteAsync(receiveBuffer, 0, receiveResult.Count, cancellationToken).ConfigureAwait(false);
                        if (receiveResult.EndOfMessage)
                        {
                            return inputStream.ToArray();
                        }
                    }
                }
            }
            finally
            {
                await inputStream.DisposeAsync().ConfigureAwait(false);
                ArrayPool<byte>.Shared.Return(receiveBuffer);
                context.Waiter.Release();
            }
            return null;
        }


        /// <summary>
        /// Close the engine.
        /// </summary>
        /// <returns>Void</returns>
        public async Task CloseAsync()
        {
            foreach (var contextKey in _contexts.Keys.ToArray())
            {
                if(_contexts.TryRemove(contextKey, out var context))
                {
                    await context.Socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None).ConfigureAwait(false);
                    context.Dispose();
                }
            }
        }
    }
}
