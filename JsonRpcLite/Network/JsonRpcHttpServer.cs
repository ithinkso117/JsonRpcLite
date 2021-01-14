using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public class JsonRpcHttpServer : IDisposable
    {
        private readonly ManualResetEvent _stopEvent = new(true);
        private readonly JsonRpcHttpRouter _router;
        private HttpListener _listener;
        private bool _disposed;

        public JsonRpcHttpServer(string serverName = "")
        {
            _router = new JsonRpcHttpRouter(serverName);
        }

        ~JsonRpcHttpServer()
        {
            DoDispose();
        }

        /// <summary>
        /// Start the rpc http server.
        /// </summary>
        /// <param name="port">The port which the server will listen.</param>
        public void Start(int port)
        {
            _stopEvent.Reset();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{port}/");
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
            Logger.WriteInfo($"JsonRpc HttpServer started on port:{port}");
        }

        private async void HandleContextAsync(HttpListenerContext context)
        {
            try
            {
                await _router.DispatchCallAsync(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Dispatch call error:{ex.Format()}");
            }
        }

        /// <summary>
        /// Stop the rpc http server
        /// </summary>
        public void Stop()
        {
            _stopEvent.Set();
            _listener.Close();
            Logger.WriteInfo($"JsonRpc HttpServer stopped.");
        }


        private void DoDispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }
    }
}
