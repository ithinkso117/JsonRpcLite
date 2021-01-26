using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Network
{
    /// <summary>
    /// JsonRpcHttpRouter will find all implemented service which inherit from the JsonRpcService, and register into itself.
    /// </summary>
    internal class JsonRpcHttpRouter
    {
        private readonly ISmdHandler _smdHandler;
        private readonly Dictionary<string, JsonRpcService> _services = new();
        private readonly Dictionary<string, IJsonRpcDispatcher> _dispatchers = new();

        private readonly string _serverName;
        private readonly bool _enableSmd;

        public JsonRpcHttpRouter(string serverName = "", bool enableSmd = true)
        {
            _serverName = serverName ?? string.Empty;
            _serverName = _serverName.ToLower();
            _enableSmd = enableSmd;
            _smdHandler = new SmdHandler();
            _dispatchers.Add("http", new JsonRpcHttpDispatcher());
            _dispatchers.Add("websocket", new JsonRpcWebsocketDispatcher());
            RegisterServices();
            Logger.WriteInfo($"JsonRpc Router created with application name:[{serverName}]");
        }



        /// <summary>
        /// Register all services which implemented by users
        /// </summary>
        private void RegisterServices()
        {
            var serviceTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(x => typeof(JsonRpcService).IsAssignableFrom(x) && x != typeof(JsonRpcService)).ToArray();
            if (serviceTypes != null)
            {
                foreach (var serviceType in serviceTypes)
                {
                    var service = (JsonRpcService)serviceType.New();
                    _services.Add(service.Name, service);
                    Logger.WriteInfo($"Register service:{service.Name}");
                }
            }
        }

        /// <summary>
        /// Parser the request url, get the calling information.
        /// </summary>
        /// <param name="requestUri">The uri requested by the caller.</param>
        /// <returns>The JsonRpcServiceInfo parsed from the uri.</returns>
        private JsonRpcServiceInfo GetRpcServiceInfo(Uri requestUri)
        {
            var serverName = string.Empty;
            string serviceName;
            bool isSmdRequest = false;
            var url = $"{requestUri.AbsolutePath.Trim('/')}";
            var urlParts = url.Split('/');
            if (urlParts.Length < 1 || urlParts.Length > 2) return null;
            if (urlParts.Length == 2)
            {
                //serverName/ServiceName
                serverName = urlParts[0].ToLower();
                serviceName = urlParts[1].ToLower();
                if (serviceName.Contains('.') && serviceName.EndsWith("smd"))
                {
                    serviceName = serviceName.Replace(".smd", String.Empty);
                    isSmdRequest = true;
                }
            }
            else
            {
                serviceName = urlParts[0].ToLower();
                if (serviceName.Contains('.') && serviceName.EndsWith("smd"))
                {
                    serviceName = serviceName.Replace(".smd", String.Empty);
                    isSmdRequest = true;
                }
            }

            //Check if the application is matched.
            return _serverName != serverName ? null : new JsonRpcServiceInfo(serviceName, isSmdRequest);
        }


        /// <summary>
        /// Dispatch the http context to the router.
        /// </summary>
        /// <param name="httpListenerContext">The context to be dispatched.</param>
        public async Task DispatchCallAsync(HttpListenerContext httpListenerContext)
        {
            IJsonRpcDispatcher dispatcher;
            object context;
            if (httpListenerContext.Request.IsWebSocketRequest)
            {
                context = await httpListenerContext.AcceptWebSocketAsync("jsonrpc");
                dispatcher = _dispatchers["websocket"];
            }
            else
            {
                context = httpListenerContext;
                dispatcher = _dispatchers["http"];
            }

            try
            {
                var serviceInfo = GetRpcServiceInfo(httpListenerContext.Request.Url);
                if (serviceInfo == null)
                {
                    Logger.WriteWarning($"Service for request: {httpListenerContext.Request.Url} not found.");
                    throw new ServerErrorException("Service does not exist.", $"Service [{null}] does not exist.");
                }

                var key = serviceInfo.Name;
                if (!_services.TryGetValue(key, out var service))
                {
                    Logger.WriteWarning($"Service for request: {httpListenerContext.Request.Url} not found.");
                    throw new ServerErrorException("Service does not exist.", $"Service [{key}] does not exist.");
                }

                var httpMethod = httpListenerContext.Request.HttpMethod.ToLower();
                if (_enableSmd && serviceInfo.IsSmdRequest && httpMethod == "get")
                {
                    context = httpListenerContext;
                    await _smdHandler.HandleAsync(service, context);
                }
                else
                {
                    await dispatcher.DispatchCallAsync(service, context);
                }
            }
            catch (Exception ex)
            {
                var response = new JsonRpcExceptionResponse();
                if (ex is RpcException rpcException)
                {
                    Logger.WriteError($"Handle request error: {rpcException.Format()}");
                    response.WriteResult(rpcException);
                }
                else
                {
                    Logger.WriteError($"Handle request error: {ex.Format()}");
                    var serverError = new InternalErrorException();
                    response.WriteResult(serverError);
                }

                await dispatcher.WriteResponseAsync(context, response);
            }
        }
    }
}
