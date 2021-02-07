using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Log;
using JsonRpcLite.Rpc;
using JsonRpcLite.Services;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.InProcess
{
    public class JsonRpcInProcessEngine:IJsonRpcServerEngine,IJsonRpcClientEngine
    {
        private IJsonRpcRouter _router;


        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; }

        public JsonRpcInProcessEngine()
        {
            Name = nameof(JsonRpcInProcessEngine);
        }


        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        public void Start(IJsonRpcRouter router)
        {
            _router = router;
        }

        /// <summary>
        /// Stop the engine.
        /// </summary>
        public void Stop()
        {
            _router = null;
        }

        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response string.</returns>
        public async Task<string> ProcessAsync(string serviceName, string requestString, CancellationToken cancellationToken)
        {
            if (_router == null) throw new NullReferenceException("The router is null");
            if (Logger.DebugMode)
            {
                Logger.WriteDebug($"Receive request data:{requestString}");
            }
            using var utf8StringData = Utf8StringData.Get(requestString);
            var requestStream = utf8StringData.Stream;
            var requests = await JsonRpcCodec.DecodeRequestsAsync(requestStream, cancellationToken).ConfigureAwait(false);
            var responses = await _router.DispatchRequestsAsync(serviceName, requests, cancellationToken).ConfigureAwait(false);
            var responseData = await JsonRpcCodec.EncodeResponsesAsync(responses, cancellationToken).ConfigureAwait(false);
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
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData, CancellationToken cancellationToken)
        {
            if (_router == null) throw new NullReferenceException("The router is null");
            if (Logger.DebugMode)
            {
                var requestString = Encoding.UTF8.GetString(requestData);
                Logger.WriteDebug($"Receive request data:{requestString}");
            }

            await using var requestStream = new MemoryStream(requestData);
            var requests = await JsonRpcCodec.DecodeRequestsAsync(requestStream, cancellationToken).ConfigureAwait(false);
            var responses = await _router.DispatchRequestsAsync(serviceName, requests, cancellationToken).ConfigureAwait(false);
            var responseData = await JsonRpcCodec.EncodeResponsesAsync(responses, cancellationToken).ConfigureAwait(false);
            if (Logger.DebugMode)
            {
                var responseString = Encoding.UTF8.GetString(responseData);
                Logger.WriteDebug($"Response data sent:{responseString}");
            }
            return responseData;
        }


        /// <summary>
        /// Close the engine.
        /// </summary>
        /// <returns>Void</returns>
        public async Task CloseAsync()
        {
            await Task.Delay(1);
        }
    }
}
