using System.Net;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JsonRpcLite.Kestrel
{
    public class JsonRpcKestrelHttpServerEngine : JsonRpcHttpServerEngineBase
    {
        private readonly IPAddress _address;
        private readonly int _port;

        private IJsonRpcRouter _router;
        private IWebHost _host;


        /// <summary>
        /// Gets whether the smd function is enabled.
        /// </summary>
        protected override bool SmdEnabled { get; }


        public JsonRpcKestrelHttpServerEngine(IPAddress address, int port, bool enableSmd = true)
        {
            Name = nameof(JsonRpcKestrelHttpServerEngine);
            _address = address;
            _port = port;
            SmdEnabled = enableSmd;
        }

        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public override async void Start(IJsonRpcRouter router)
        {
            _router = router;
            var builder = new WebHostBuilder()
                .SuppressStatusMessages(true)
                .UseKestrel(options =>
                {
                    options.Listen(_address, _port);
                })
                .ConfigureLogging((_, logging) =>
                {
                    logging.ClearProviders();
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        var jsonRpcHttpContext = new JsonRpcKestrelHttpContext(context);
                        return HandleContextAsync(jsonRpcHttpContext, _router);
                    });
                });
            _host = builder.Build();
            await _host.RunAsync().ConfigureAwait(false);
        }


        /// <summary>
        /// Stop the engine.
        /// </summary>
        public override async void Stop()
        {
            if (_host != null)
            {
                await _host.StopAsync().ConfigureAwait(false);
                _host.Dispose();
                Logger.WriteInfo("JsonRpc kestrel http server engine stopped.");
            }
        }

    }
}
