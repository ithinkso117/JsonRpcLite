using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Rpc
{
    public class JsonRpcClient:IDisposable,IAsyncDisposable
    {
        private uint _requestId;

        private IJsonRpcClientEngine _engine;
        private bool _disposed;


        ~JsonRpcClient()
        {
            Dispose();
        }

        /// <summary>
        /// Use given engine to handle the request.
        /// </summary>
        /// <param name="engine">The engine for server.</param>
        public void UseEngine(IJsonRpcClientEngine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Create a proxy for given interface.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        /// <param name="timeout">The timeout of the proxy.</param>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The proxy which implement the given interface.</returns>
        public T CreateProxy<T>(int timeout = Timeout.Infinite, string serviceName = null)
        {
            var interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException($"{nameof(T)} is not an interface");
            }
            var name = serviceName;
            if (string.IsNullOrEmpty(serviceName))
            {
                name = interfaceType.Name;
                //Remove the start "I" char for the interface name if exists.
                if (name.StartsWith("i", StringComparison.InvariantCultureIgnoreCase) && name.Length > 1)
                {
                    name = name.Substring(1);
                }

                var serviceAttributes = interfaceType.GetCustomAttributes(typeof(RpcServiceAttribute), false);
                if (serviceAttributes.Length > 1)
                {
                    throw new InvalidOperationException($"Service {interfaceType.Name} defined more than one rpc service attributes.");
                }
                if (serviceAttributes.Length > 0)
                {
                    var serviceAttribute = (RpcServiceAttribute)serviceAttributes[0];
                    if (!string.IsNullOrEmpty(serviceAttribute.Name))
                    {
                        name = serviceAttribute.Name;
                    }
                }
            }
            return JsonRpcClientProxy.CreateProxy<T>(name, this, timeout);
        }

        /// <summary>
        /// Invoke remote method and get the result.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The result value.</returns>
        public async Task<T> InvokeAsync<T>(string serviceName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            var id = Interlocked.Increment(ref _requestId);
            var request = new JsonRpcRequest(id, methodName, new JsonRpcRequestParameter(RequestParameterType.Object, args));
            var requestData = await JsonRpcCodec.EncodeRequestsAsync(new[] { request }, cancellationToken).ConfigureAwait(false);
            var responseData = await ProcessAsync(serviceName, requestData, cancellationToken).ConfigureAwait(false);
            var responses = await JsonRpcCodec.DecodeResponsesAsync(responseData, cancellationToken).ConfigureAwait(false);
            if (responses.Length > 0)
            {
                var response = responses[0];
                var responseId = Convert.ToInt32(response.Id);
                if (responseId != id)
                {
                    throw new InvalidOperationException("Response id is not matched.");
                }
                if (response.Result is RpcException exception)
                {
                    throw exception;
                }

                var resultString = (string)response.Result;
                using var utf8StringData = Utf8StringData.Get(resultString);
                return await JsonSerializer.DeserializeAsync<T>(utf8StringData.Stream,JsonRpcConvertSettings.SerializerOptions, cancellationToken).ConfigureAwait(false);

            }

            throw new InvalidOperationException("Fail to get invoke result from server.");
        }


        /// <summary>
        /// Invoke remote method without result.
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>Void</returns>
        public async Task VoidInvokeAsync(string serviceName, string methodName, CancellationToken cancellationToken, params object[] args)
        {
            var id = Interlocked.Increment(ref _requestId);
            var request = new JsonRpcRequest(id, methodName, new JsonRpcRequestParameter(RequestParameterType.Object, args));
            var requestData = await JsonRpcCodec.EncodeRequestsAsync(new[] { request }, cancellationToken).ConfigureAwait(false);
            var responseData = await ProcessAsync(serviceName, requestData, cancellationToken).ConfigureAwait(false);
            var responses = await JsonRpcCodec.DecodeResponsesAsync(responseData, cancellationToken).ConfigureAwait(false);
            if (responses.Length > 0)
            {
                var response = responses[0];
                var responseId = Convert.ToInt32(response.Id);
                if (responseId != id)
                {
                    throw new InvalidOperationException("Response id is not matched.");
                }
                if (response.Result is RpcException exception)
                {
                    throw exception;
                }
                var resultString = (string)response.Result;
                if (resultString != "null")
                {
                    throw new InvalidOperationException("The result from server is not [null]");
                }
            }
            else
            {
                throw new InvalidOperationException("Fail to get invoke result from server.");
            }
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
            if (_engine == null) throw new NullReferenceException("The engine is null.");
            return await _engine.ProcessAsync(serviceName, requestString, cancellationToken);
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
            if (_engine == null) throw new NullReferenceException("The engine is null.");
            return await _engine.ProcessAsync(serviceName, requestData, cancellationToken);
        }

        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, string requestString, CancellationToken cancellationToken = default)
        {
            await ProcessAsync(serviceName, requestString, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, byte[] requestData, CancellationToken cancellationToken = default)
        {
            await ProcessAsync(serviceName, requestData, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Close and release resource of the client.
        /// </summary>
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }

        /// <summary>
        /// Close and release resource of the client.
        /// </summary>
        /// <returns>Void</returns>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_engine != null)
                {
                    await _engine.CloseAsync();
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
