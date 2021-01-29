using System;
using System.Net.Http;
using System.Threading.Tasks;
using JsonRpcLite.Rpc;

namespace JsonRpcLite.Network
{
    public class JsonRpcHttpClientEngine:IJsonRpcClientEngine
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Gets the engine name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the timeout of the engine.
        /// </summary>
        public int Timeout
        {
            get => (int)_httpClient.Timeout.TotalMilliseconds;
            set => _httpClient.Timeout = TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// Gets or sets the max response buffer size.
        /// </summary>
        public long MaxResponseContentBufferSize
        {
            get => _httpClient.MaxResponseContentBufferSize;
            set => _httpClient.MaxResponseContentBufferSize = value;
        }

        public JsonRpcHttpClientEngine(string serverUrl)
        {
            Name = nameof(JsonRpcHttpClientEngine);
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
            };
        }


        /// <summary>
        /// Process a string request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestString">The request string</param>
        /// <returns>The response string.</returns>
        public async Task<string> ProcessAsync(string serviceName, string requestString)
        {
            var content = new StringContent(requestString);
            var response = await _httpClient.PostAsync($"/{serviceName}", content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new InvalidOperationException($"Fail to get result from server :{(int) response.StatusCode}:{response.ReasonPhrase}");
        }


        /// <summary>
        /// Process a byte[] request which contains the json data.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>The response data.</returns>
        public async Task<byte[]> ProcessAsync(string serviceName, byte[] requestData)
        {
            var content = new ByteArrayContent(requestData);
            var response = await _httpClient.PostAsync($"/{serviceName}", content).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            throw new InvalidOperationException($"Fail to get result from server :{(int)response.StatusCode}:{response.ReasonPhrase}");
        }
    }
}
