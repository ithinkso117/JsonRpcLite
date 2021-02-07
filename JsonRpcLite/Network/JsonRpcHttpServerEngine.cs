using System;
using System.Net;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    public class JsonRpcHttpServerEngine : JsonRpcHttpServerEngineBase
    {
        private readonly string _prefix;

        private bool _stopped;

        private HttpListener _listener;

        private IJsonRpcRouter _router;

        /// <summary>
        /// Gets whether the smd function is enabled.
        /// </summary>
        protected override bool SmdEnabled { get; }


        public JsonRpcHttpServerEngine(string prefix, bool enableSmd = true)
        {
            Name = nameof(JsonRpcHttpServerEngine);
            _prefix = prefix;
            SmdEnabled = enableSmd;
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
        public override void Stop()
        {
            _router = null;
            _stopped = true;
            _listener.Close();
            Logger.WriteInfo("JsonRpc http server engine stopped.");
        }

        
        /// <summary>
        /// Call base HandleContextAsync.
        /// </summary>
        /// <param name="context">The http listener context</param>
        private async void HandleContextAsync(HttpListenerContext context)
        {
            var jsonRpcHttpContext = new JsonRpcHttpListenerContext(context);
            await HandleContextAsync(jsonRpcHttpContext, _router);
        }
    }
}
