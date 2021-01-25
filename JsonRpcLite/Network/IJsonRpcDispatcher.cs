using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Network
{
    internal interface IJsonRpcDispatcher
    {
        /// <summary>
        /// Dispatch request to different services.
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The context for communication</param>
        /// <returns>Void</returns>
        Task DispatchCallAsync(JsonRpcService service, object context);

        /// <summary>
        /// Write response to remote side.
        /// </summary>
        /// <param name="context">The context for communication</param>
        /// <param name="response">The response to write</param>
        /// <returns>Void</returns>
        Task WriteResponseAsync(object context, JsonRpcResponse response);

        /// <summary>
        /// Write response to remote side.
        /// </summary>
        /// <param name="context">The context for communication</param>
        /// <param name="responses">The responses to write</param>
        /// <returns>Void</returns>
        Task WriteResponsesAsync(object context, JsonRpcResponse[] responses);
    }
}
