using System.Net;
using System.Threading.Tasks;
using JsonRpcLite.Network;
using JsonRpcLite.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JsonRpcLite.Kestrel
{
    public class JsonRpcKestrelWebSocketServerEngine:JsonRpcWebSocketServerEngineBase
    {
        private readonly IPAddress _address;
        private readonly int _port;

        private IJsonRpcRouter _router;
        private IWebHost _host;

        public JsonRpcKestrelWebSocketServerEngine(IPAddress address, int port)
        {
            Name = nameof(JsonRpcKestrelWebSocketServerEngine);
            _address = address;
            _port = port;
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
                    app.UseWebSockets();
                    app.Run(HandleRequestAsync);
                });
            _host = builder.Build();
            await _host.RunAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Handle the http context from kestrel
        /// </summary>
        /// <param name="context">The context to handle.</param>
        /// <returns>Void</returns>
        private async Task HandleRequestAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var requestPath = context.Request.Path;
                var socket = await context.WebSockets.AcceptWebSocketAsync("JsonRpcLite").ConfigureAwait(false);
                await HandleRequestAsync(requestPath, _router, socket).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        /// <summary>
        /// Stop the engine.
        /// </summary>
        public override async void Stop()
        {
            await _host.StopAsync().ConfigureAwait(false);
            _host.Dispose();
        }
    }
}
