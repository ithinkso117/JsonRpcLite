using System.Threading.Tasks;

namespace JsonRpcLite.InProcess
{
    /// <summary>
    /// An in-process JsonRpcServer, can be called internally.
    /// </summary>
    public class JsonRpcInProcessServer
    {
        private readonly JsonRpcInProcessRouter _router;
        public JsonRpcInProcessServer()
        {
            _router = new JsonRpcInProcessRouter();
        }

        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="serviceVersion">The version of the service.</param>
        /// <param name="request">The request string</param>
        /// <returns>The response string.</returns>
        public async Task ProcessAsync(string serviceName, string serviceVersion, string request)
        {
            await _router.DispatchCallAsync(serviceName, serviceVersion, request);
        }


        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="serviceVersion">The version of the service.</param>
        /// <param name="request">The request data</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, string serviceVersion, byte[] request)
        {
            return await _router.DispatchCallAsync(serviceName, serviceVersion, request);
        }
    }
}
