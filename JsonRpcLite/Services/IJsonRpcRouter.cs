using System.Threading.Tasks;

namespace JsonRpcLite.Services
{
    public interface IJsonRpcRouter
    {
        /// <summary>
        /// Check whether the service exists.
        /// </summary>
        /// <param name="serviceName">The service name.</param>
        /// <returns>True exists otherwise false.</returns>
        bool ServiceExists(string serviceName);

        /// <summary>
        /// Get service's SMD data by service name.
        /// </summary>
        /// <param name="serviceName">The service name to handle.</param>
        /// <returns>The SMD data for the service.</returns>
        Task<byte[]> GetServiceSmdData(string serviceName);

        /// <summary>
        /// Dispatch the requests and get the responses.
        /// </summary>
        /// <param name="serviceName">The service name to dispatch.</param>
        /// <param name="requests">The requests to handle.</param>
        /// <returns>The handled response.</returns>
        Task<JsonRpcResponse[]> DispatchRequestsAsync(string serviceName, JsonRpcRequest[] requests);
    }
}
