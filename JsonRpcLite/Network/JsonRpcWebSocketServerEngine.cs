using System;
using System.Net;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public class JsonRpcWebSocketServerEngine : JsonRpcWebSocketServerEngineBase
    {
        private readonly string _prefix;

        private HttpListener _listener;
        private IJsonRpcRouter _router;
        private bool _stopped;


        public JsonRpcWebSocketServerEngine(string prefix)
        {
            Name = nameof(JsonRpcWebSocketServerEngine);
            _prefix = prefix;
        }


        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public override void Start(IJsonRpcRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _stopped = false;
            _listener = new HttpListener();
            _listener.Prefixes.Add(_prefix);
            _listener.UnsafeConnectionNtlmAuthentication = true;
            _listener.IgnoreWriteExceptions = true;
            _listener.Start();
            Task.Factory.StartNew(async () =>
            {
                while (!_stopped)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync().ConfigureAwait(false);
                        if (context.Request.IsWebSocketRequest)
                        {
                            var requestPath = string.Empty;
                            if (context.Request.Url != null)
                            {
                                requestPath = context.Request.Url.AbsolutePath;
                            }
                            var websocketContext = await context.AcceptWebSocketAsync("JsonRpcLite").ConfigureAwait(false);
                            ProcessRequestAsync(requestPath, _router, websocketContext.WebSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteError($"GetContext error:{ex.Format()}");
                    }
                }
            }, TaskCreationOptions.LongRunning);
            Logger.WriteInfo("JsonRpc websocket server engine started.");
        }


        /// <summary>
        /// Stop the engine.
        /// </summary>
        public override void Stop()
        {
            _router = null;
            _stopped = true;
            _listener.Close();
            Logger.WriteInfo("JsonRpc websocket server engine stopped.");
        }
    }
}
