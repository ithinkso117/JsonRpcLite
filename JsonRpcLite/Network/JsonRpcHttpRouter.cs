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
    /// JsonRpcHttpRouter will find all implemented handler which inherit from the BaseHandler, and register into itself.
    /// </summary>
    internal class JsonRpcHttpRouter
    {
        private readonly Dictionary<string, JsonRpcService> _services = new();
        private readonly Dictionary<string, IJsonRpcDispatcher> _dispatchers = new();

        private readonly string _serverName;

        public JsonRpcHttpRouter(string serverName = "")
        {
            _serverName = serverName ?? string.Empty;
            _serverName = _serverName.ToLower();
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
                    var serviceAttributes = serviceType.GetCustomAttributes(typeof(RpcServiceAttribute), false);
                    if (serviceAttributes.Length > 1)
                    {
                        throw new InvalidOperationException($"Method {serviceType.Name} defined more than one rpc service attributes.");
                    }

                    if (serviceAttributes.Length > 0)
                    {
                        var serviceAttribute = (RpcServiceAttribute) serviceAttributes[0];
                        if (string.IsNullOrEmpty(serviceAttribute.Name)) continue;
                        var key = $"{serviceAttribute.Name.ToLower()}";
                        if (!string.IsNullOrWhiteSpace(serviceAttribute.Version))
                        {
                            key = $"{serviceAttribute.Name.ToLower()}/{serviceAttribute.Version}";
                        }
                        _services.Add(key, (JsonRpcService)serviceType.New());
                        Logger.WriteInfo($"Register service:{key}");
                    }
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
            string version;
            string serviceName;
            var url = $"{requestUri.AbsolutePath.Trim('/')}";
            var urlParts = url.Split('/');
            if (!string.IsNullOrEmpty(_serverName))
            {
                if (urlParts.Length < 2 || urlParts.Length > 3) return null;
                if (urlParts.Length == 3)
                {
                    //serverName/ServiceName/Version
                    serverName = urlParts[0].ToLower();
                    serviceName = urlParts[1].ToLower();
                    version = urlParts[2].ToLower();
                }
                else
                {
                    //serverName/ServiceName
                    serverName = urlParts[0].ToLower();
                    serviceName = urlParts[1].ToLower();
                    version = "v1";
                }
            }
            else
            {
                if (urlParts.Length < 1 || urlParts.Length > 2) return null;
                if (urlParts.Length == 2)
                {
                    //ServiceName/Version
                    serviceName = urlParts[0].ToLower();
                    version = urlParts[1].ToLower();
                }
                else
                {
                    //ServiceName
                    serviceName = urlParts[0].ToLower();
                    version = "v1";
                }
            }
            //Check if the application is matched.
            return _serverName != serverName ? null : new JsonRpcServiceInfo(serviceName, version);
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

                var key = $"{serviceInfo.Name}/{serviceInfo.Version}";
                if (!_services.TryGetValue(key, out var service))
                {
                    Logger.WriteWarning($"Service for request: {httpListenerContext.Request.Url} not found.");
                    throw new ServerErrorException("Service does not exist.", $"Service [{key}] does not exist.");
                }
                await dispatcher.DispatchCall(service, context);
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
                await dispatcher.WriteResponse(context, response);
            }
        }
    }
}
