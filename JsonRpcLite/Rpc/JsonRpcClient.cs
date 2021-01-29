using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Rpc
{
    public class JsonRpcClient
    {
        private uint _requestId;

        private IJsonRpcClientEngine _engine;

        /// <summary>
        /// Use given engine to handle the request.
        /// </summary>
        /// <param name="engine">The engine for server.</param>
        public void UseEngine(IJsonRpcClientEngine engine)
        {
            _engine = engine;
        }

        public T CreateProxy<T>(string serviceName)
        {
            return JsonRpcClientProxy.CreateProxy<T>(serviceName, this);
        }

        /// <summary>
        /// Invoke remote method and get the result.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>The result value.</returns>
        public async Task<T> InvokeAsync<T>(string serviceName, string methodName, params object[] args)
        {
            var id = Interlocked.Increment(ref _requestId);
            var request = new JsonRpcRequest(id, methodName, new JsonRpcRequestParameter(RequestParameterType.Object, args));
            var requestData = await JsonRpcCodec.EncodeRequestsAsync(new[] { request }).ConfigureAwait(false);
            var responseData = await ProcessAsync(serviceName, requestData).ConfigureAwait(false);
            var responses = await JsonRpcCodec.DecodeResponsesAsync(responseData).ConfigureAwait(false);
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
                return await JsonSerializer.DeserializeAsync<T>(utf8StringData.Stream,JsonRpcConvertSettings.SerializerOptions).ConfigureAwait(false);

            }

            throw new InvalidOperationException("Fail to get invoke result from server.");
        }


        /// <summary>
        /// Invoke remote method without result.
        /// </summary>
        /// <param name="serviceName">The name of the service</param>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>Void</returns>
        public async Task VoidInvokeAsync(string serviceName, string methodName, params object[] args)
        {
            var id = Interlocked.Increment(ref _requestId);
            var request = new JsonRpcRequest(id, methodName, new JsonRpcRequestParameter(RequestParameterType.Object, args));
            var requestData = await JsonRpcCodec.EncodeRequestsAsync(new[] { request }).ConfigureAwait(false);
            var responseData = await ProcessAsync(serviceName, requestData).ConfigureAwait(false);
            var responses = await JsonRpcCodec.DecodeResponsesAsync(responseData).ConfigureAwait(false);
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
        /// <returns>The response string.</returns>
        public async Task<string> ProcessAsync(string serviceName, string requestString)
        {
            if (_engine == null) throw new NullReferenceException("The engine is null.");
            return await _engine.ProcessAsync(serviceName, requestString);
        }

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData)
        {
            if (_engine == null) throw new NullReferenceException("The engine is null.");
            return await _engine.ProcessAsync(serviceName, requestData);
        }

        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, string requestString)
        {
            await ProcessAsync(serviceName, requestString).ConfigureAwait(false);
        }


        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, byte[] requestData)
        {
            await ProcessAsync(serviceName, requestData).ConfigureAwait(false);
        }
    }
}
