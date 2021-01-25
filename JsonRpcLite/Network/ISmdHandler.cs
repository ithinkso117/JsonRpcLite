using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Network
{
    internal interface ISmdHandler
    {
        /// <summary>
        /// Dispatch request to smd .
        /// </summary>
        /// <param name="service">The service to handle the context</param>
        /// <param name="context">The context for communication</param>
        /// <returns>Void</returns>
        Task HandleAsync(JsonRpcService service, object context);
    }
}
