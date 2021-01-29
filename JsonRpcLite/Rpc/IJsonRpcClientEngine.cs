using System.Threading.Tasks;

namespace JsonRpcLite.Rpc
{
    public interface IJsonRpcClientEngine
    {
        /// <summary>
        /// Gets the engine name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>The response string.</returns>
        Task<string> ProcessAsync(string serviceName, string requestString);

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The response data.</returns>
        Task<byte[]> ProcessAsync(string serviceName, byte[] requestData);
    }
}
