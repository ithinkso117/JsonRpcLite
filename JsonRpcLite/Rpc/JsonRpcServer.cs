using System;
using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Rpc
{
    public class JsonRpcServer
    {
        private JsonRpcServerEngine _engine;
        private readonly JsonRpcServiceRouter _router = new JsonRpcServiceRouter();

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
        public void UseEngine(JsonRpcServerEngine engine)
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


        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>The response string.</returns>
        public async Task<string> ProcessAsync(string serviceName, string requestString)
        {
            if (_engine == null) throw new NullReferenceException("The server engine is null");
            return await _engine.ProcessAsync(serviceName, requestString).ConfigureAwait(false);
        }

        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData)
        {
            if (_engine == null) throw new NullReferenceException("The server engine is null");
            return await _engine.ProcessAsync(serviceName, requestData).ConfigureAwait(false);
        }

        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, string requestString)
        {
            if (_engine == null) throw new NullReferenceException("The server engine is null");
            await _engine.BenchmarkAsync(serviceName, requestString).ConfigureAwait(false);
        }


        /// <summary>
        /// Process a string request which contains the json data, return nothing for benchmark..
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>Void</returns>
        public async Task BenchmarkAsync(string serviceName, byte[] requestData)
        {
            if (_engine == null) throw new NullReferenceException("The server engine is null");
            await _engine.BenchmarkAsync(serviceName, requestData).ConfigureAwait(false);
        }
    }
}
