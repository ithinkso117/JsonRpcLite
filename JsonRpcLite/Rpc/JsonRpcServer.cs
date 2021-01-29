using System;
using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Rpc
{
    public class JsonRpcServer
    {
        private IJsonRpcServerEngine _engine;
        private readonly JsonRpcServiceRouter _router = new();

        /// <summary>
        /// Register a service with its interface.
        /// </summary>
        /// <typeparam name="T">The interface of the service.</typeparam>
        /// <param name="service">The service to register.</param>
        public void RegisterService<T>(T service)
        {
            _router.RegisterService<T>(service);
        }

        /// <summary>
        /// Use given engine to handle the request.
        /// </summary>
        /// <param name="engine">The engine for server.</param>
        public void UseEngine(IJsonRpcServerEngine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start()
        {
            if (_engine == null)
            {
                throw new InvalidOperationException("Engine does not exist.");
            }
            _engine.Start(_router);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (_engine == null)
            {
                throw new InvalidOperationException("Engine does not exist.");
            }
            _engine.Stop();
        }
    }
}
