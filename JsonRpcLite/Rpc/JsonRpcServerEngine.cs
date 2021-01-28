using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Rpc
{
    public class JsonRpcServerEngine
    {
        protected IJsonRpcRouter Router;

        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public virtual void Start(IJsonRpcRouter router)
        {
            Router = router ?? throw new ArgumentNullException(nameof(router));
        }


        /// <summary>
        /// Stop the engine.
        /// </summary>
        public virtual void Stop()
        {
            Router = null;
        }

        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>The response string.</returns>
        internal async Task<string> ProcessAsync(string serviceName, string requestString)
        {
            if (Router == null) throw new NullReferenceException("The router is null");
            if (Logger.DebugMode)
            {
                Logger.WriteDebug($"Receive request data:{requestString}");
            }
            using var utf8StringData = Utf8StringData.Get(requestString);
            var requestStream = utf8StringData.Stream;
            var requests = await JsonRpcCodec.DecodeRequestsAsync(requestStream).ConfigureAwait(false);
            var responses = await Router.DispatchRequestsAsync(serviceName, requests).ConfigureAwait(false);
            var responseData = await JsonRpcCodec.EncodeResponsesAsync(responses).ConfigureAwait(false);
            var responseString = Encoding.UTF8.GetString(responseData);
            if (Logger.DebugMode)
            {
                Logger.WriteDebug($"Response data sent:{responseString}");
            }
            return responseString;
        }

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The response data.</returns>
        internal async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData)
        {
            if (Router == null) throw new NullReferenceException("The router is null");
            if (Logger.DebugMode)
            {
                var requestString = Encoding.UTF8.GetString(requestData);
                Logger.WriteDebug($"Receive request data:{requestString}");
            }

            await using var requestStream = new MemoryStream(requestData);
            var requests = await JsonRpcCodec.DecodeRequestsAsync(requestStream).ConfigureAwait(false);
            var responses = await Router.DispatchRequestsAsync(serviceName, requests).ConfigureAwait(false);
            var responseData = await JsonRpcCodec.EncodeResponsesAsync(responses).ConfigureAwait(false);
            if (Logger.DebugMode)
            {
                var responseString = Encoding.UTF8.GetString(responseData);
                Logger.WriteDebug($"Response data sent:{responseString}");
            }
            return responseData;
        }

        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>Void</returns>
        internal async Task BenchmarkAsync(string serviceName, string requestString)
        {
            await ProcessAsync(serviceName, requestString).ConfigureAwait(false);
        }


        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>Void</returns>
        internal async Task BenchmarkAsync(string serviceName, byte[] requestData)
        {
            await ProcessAsync(serviceName, requestData).ConfigureAwait(false);
        }
    }
}
