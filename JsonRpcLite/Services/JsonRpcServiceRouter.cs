using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Services
{
    internal class JsonRpcServiceRouter:IJsonRpcRouter
    {
        public JsonRpcServiceRouter()
        {
            RegisterRpcServices();
        }

        /// <summary>
        /// Register a service with its interface.
        /// </summary>
        /// <typeparam name="T">The interface of the service.</typeparam>
        /// <param name="service">The service to register.</param>
        public void RegisterService<T>(T service)
        {
            JsonRpcCallManager.RegisterService(service);
        }


        /// <summary>
        /// Register exist services which marked with RpcService attribute.
        /// </summary>
        private void RegisterRpcServices()
        {
            var types = Assembly.GetEntryAssembly()?.GetTypes();
            if (types != null)
            {
                foreach (var type in types)
                {
                    var serviceAttributes = type.GetCustomAttributes(typeof(RpcServiceAttribute), false);
                    if (serviceAttributes.Length > 0)
                    {
                        var service = type.New();
                        JsonRpcCallManager.RegisterService(service);
                    }
                }
            }
        }


        /// <summary>
        /// Check whether the service exists.
        /// </summary>
        /// <param name="serviceName">The service name.</param>
        /// <returns>True exists otherwise false.</returns>
        public bool ServiceExists(string serviceName)
        {
            return JsonRpcCallManager.GetService(serviceName) != null;
        }

        /// <summary>
        /// Get service's SMD data by service name.
        /// </summary>
        /// <param name="serviceName">The service name to handle.</param>
        /// <returns>The SMD data for the service.</returns>
        public Task<byte[]> GetServiceSmdData(string serviceName)
        {
            var service = JsonRpcCallManager.GetService(serviceName);
            if (service == null)
            {
                throw new InvalidOperationException($"Service {serviceName} does not exist.");
            }
            return Task.FromResult(service.SmdData);
        }


        /// <summary>
        /// Dispatch the requests and get the responses.
        /// </summary>
        /// <param name="serviceName">The service name to dispatch.</param>
        /// <param name="requests">The requests to handle.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The handled response.</returns>
        public async Task<JsonRpcResponse[]> DispatchRequestsAsync(string serviceName, JsonRpcRequest[] requests, CancellationToken cancellationToken = default)
        {
            var service = JsonRpcCallManager.GetService(serviceName);
            if (service == null)
            {
                throw new InvalidOperationException($"Service {serviceName} does not exist.");
            }
            if (requests.Length == 1)
            {
                var request = requests[0];
                var response = await GetResponseAsync(service, request, cancellationToken).ConfigureAwait(false);
                return new[]{response};

            }
            //batch call.
            var responseList = new List<JsonRpcResponse>();
            foreach (var request in requests)
            {
                var response = await GetResponseAsync(service, request, cancellationToken).ConfigureAwait(false);
                if (response != null)
                {
                    responseList.Add(response);
                }
            }

            return responseList.ToArray();
        }


        /// <summary>
        /// Handle request request and get the response
        /// </summary>
        /// <param name="service">The service which will handle the request.</param>
        /// <param name="request">The request to handle.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response for the request.</returns>
        private async Task<JsonRpcResponse> GetResponseAsync(IJsonRpcCallService service, JsonRpcRequest request, CancellationToken cancellationToken)
        {
            JsonRpcResponse response = null;
            try
            {
                var rpcCall = service[request.Method];
                if (rpcCall == null)
                {
                    throw new MethodNotFoundException($"Method: {request.Method} not found.");
                }

                object[] arguments;
                if (request.Params.Type == RequestParameterType.RawString)
                {
                    var paramString = (string) request.Params.Value;
                    arguments = await JsonRpcCodec.DecodeArgumentsAsync(paramString, rpcCall.Parameters, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (request.Params.Value is Array array)
                    {
                        arguments = array.Cast<object>().ToArray();
                    }
                    else
                    {
                        arguments = new[] {request.Params.Value};
                    }
                }

                //From here we got the response id.
                response = new JsonRpcResponse(request.Id);
                if (arguments.Length == rpcCall.Parameters.Count)
                {
                    try
                    {
                        var result = await rpcCall.Call(arguments).ConfigureAwait(false);
                        if (request.IsNotification)
                        {
                            return null;
                        }
                        response.WriteResult(result);
                    }
                    catch (Exception ex)
                    {
                        var argumentString = new StringBuilder();
                        argumentString.Append(Environment.NewLine);
                        var index = 0;
                        foreach (var argument in arguments)
                        {
                            argumentString.AppendLine($"[{index}] {argument}");
                        }
                        argumentString.Append(Environment.NewLine);
                        response.WriteResult(new InternalErrorException($"Call method {rpcCall.Name} with args:{argumentString} error :{ex.Format()}"));
                    }
                }
                else
                {
                    throw new InvalidParamsException("Argument count is not matched");
                }
            }
            catch (Exception ex)
            {
                response ??= new JsonRpcResponse();
                if (ex is RpcException rpcException)
                {
                    response.WriteResult(rpcException);
                }
                else
                {
                    var serverError = new InternalErrorException($"Handle request {request} error: {ex.Format()}");
                    response.WriteResult(serverError);
                }

            }
            return response;
        }
    }
}
