using System.Threading;
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
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response string.</returns>
        Task<string> ProcessAsync(string serviceName, string requestString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <param name="cancellationToken">The cancellation token which can cancel this method.</param>
        /// <returns>The response data.</returns>
        Task<byte[]> ProcessAsync(string serviceName, byte[] requestData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close the engine.
        /// </summary>
        /// <returns>Void</returns>
        Task CloseAsync();
    }
}
