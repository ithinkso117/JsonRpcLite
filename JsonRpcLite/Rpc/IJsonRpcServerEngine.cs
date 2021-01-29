using JsonRpcLite.Services;

namespace JsonRpcLite.Rpc
{
    public interface IJsonRpcServerEngine
    {
        /// <summary>
        /// Gets the engine name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Start the engine and use given router to handle request.
        /// </summary>
        /// <param name="router">The router which will handle the request.</param>
        void Start(IJsonRpcRouter router);


        /// <summary>
        /// Stop the engine.
        /// </summary>
        void Stop();
    }
}
