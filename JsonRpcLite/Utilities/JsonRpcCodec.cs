using System;
using JsonRpcLite.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonRpcLite.Utilities
{
    public class JsonRpcCodec
    {
        private static readonly ObjectPool<JsonRpcRequestData> RequestDataPool = new(() => new JsonRpcRequestData());
        private static readonly ObjectPool<JsonRpcResponseData> ResponseDataPool = new(() => new JsonRpcResponseData());


        /// <summary>
        /// Convert one JsonElement to a JsonRpcRequest
        /// </summary>
        /// <param name="element">The JsonElement to convert.</param>
        /// <returns>The converted JsonRpcRequest</returns>
        private static JsonRpcRequest DecodeRequest(JsonElement element)
        {                           
            string parameters = null;
            object id = null;
            if (element.TryGetProperty("jsonrpc", out var versionElement))
            {
                var version = versionElement.GetString();
                if (version != "2.0")
                {
                    throw new InvalidRequestException($"Invalid version property:{version}");
                }
            }
            else
            {
                throw new InvalidRequestException("Missing version property.");
            }
            if (!element.TryGetProperty("method", out var methodElement))
            {
                throw new InvalidRequestException("Missing method property.");
            }
            var method = methodElement.GetString();
            //If id is null, the method is a notification.
            if(element.TryGetProperty("id", out var idElement))
            {
                if (idElement.ValueKind == JsonValueKind.Number)
                {
                    id = idElement.GetInt32();
                }
                else if (idElement.ValueKind == JsonValueKind.String)
                {
                    id = idElement.GetString();
                }
                else
                {
                    throw new InvalidRequestException($"Invalid id property: {idElement.GetRawText()}");
                }
            }
            //If params is null, it means it is a method without parameters.
            if(element.TryGetProperty("params", out var parametersElement))
            {
                parameters = parametersElement.GetRawText();
            }
            return new JsonRpcRequest(id, method, new JsonRpcRequestParameter(RequestParameterType.RawString, parameters));
        }

        /// <summary>
        /// Convert binary data to JsonRpcRequest array.
        /// </summary>
        /// <param name="requestData">The data to be converted.</param>
        /// <param name="dataLength">The available data length of the request data. if 0 use all data.</param>
        /// <returns>The JsonRpcRequest array converted from request data.</returns>
        public static async Task<JsonRpcRequest[]> DecodeRequestsAsync(byte[] requestData, int dataLength = 0)
        {
            await using var memoryStream = dataLength == 0
                ? new MemoryStream(requestData)
                : new MemoryStream(requestData, 0, dataLength);
            JsonDocument doc = null;
            try
            {
                try
                {
                    doc = await JsonDocument.ParseAsync(memoryStream, JsonRpcConvertSettings.DocumentOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new ParseErrorException(ex.Message);
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.EnumerateArray().Select(DecodeRequest).ToArray();
                }

                return new[] {DecodeRequest(doc.RootElement)};

            }
            finally
            {
                doc?.Dispose();
            }
        }

        /// <summary>
        /// Convert binary data to JsonRpcHttpRequest array.
        /// </summary>
        /// <param name="stream">The stream contains request data to be converted.</param>
        /// <returns>The JsonRpcHttpRequest array converted from request data.</returns>
        public static async Task<JsonRpcRequest[]> DecodeRequestsAsync(Stream stream)
        {
            JsonDocument doc = null;
            try
            {
                try
                {
                    doc = await JsonDocument.ParseAsync(stream, JsonRpcConvertSettings.DocumentOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new ParseErrorException(ex.Message);
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.EnumerateArray().Select(DecodeRequest).ToArray();
                }

                return new[] { DecodeRequest(doc.RootElement) };

            }
            finally
            {
                doc?.Dispose();
            }
        }


        /// <summary>
        /// Convert one JsonElement to a JsonRpcResponse
        /// </summary>
        /// <param name="element">The JsonElement to convert.</param>
        /// <returns>The converted JsonRpcResponse</returns>
        private static JsonRpcResponse DecodeResponse(JsonElement element)
        {
            object id = null;
            object result = null;
            if (element.TryGetProperty("jsonrpc", out var versionElement))
            {
                var version = versionElement.GetString();
                if (version != "2.0")
                {
                    throw new InvalidRequestException($"Invalid version property:{version}");
                }
            }
            else
            {
                throw new InvalidRequestException("Missing version property.");
            }
            if (element.TryGetProperty("result", out var resultElement))
            {
                result = resultElement.GetRawText();
            }
            else if (element.TryGetProperty("error", out var errorElement))
            {
                result = JsonSerializer.Deserialize<RpcException>(errorElement.GetRawText(),JsonRpcConvertSettings.SerializerOptions);
            }

            if (result == null)
            {
                throw new InvalidRequestException("Missing result/error property.");
            }
            //If id is null, the method is a notification.
            if (element.TryGetProperty("id", out var idElement))
            {
                if (idElement.ValueKind == JsonValueKind.Number)
                {
                    id = idElement.GetInt32();
                }
                else if (idElement.ValueKind == JsonValueKind.String)
                {
                    id = idElement.GetString();
                }
                else
                {
                    throw new InvalidRequestException($"Invalid id property: {idElement.GetRawText()}");
                }
            }

            var response = new JsonRpcResponse(id);
            response.WriteResult(result);
            return response;
        }


        /// <summary>
        /// Convert binary data to JsonRpcResponse array.
        /// </summary>
        /// <param name="responseData">The data to be converted.</param>
        /// <param name="dataLength">The available data length of the response data. if 0 use all data.</param>
        /// <returns>The JsonRpcResponse array converted from response data.</returns>
        public static async Task<JsonRpcResponse[]> DecodeResponsesAsync(byte[] responseData, int dataLength = 0)
        {
            await using var memoryStream = dataLength == 0
                ? new MemoryStream(responseData)
                : new MemoryStream(responseData, 0, dataLength);
            JsonDocument doc = null;
            try
            {
                try
                {
                    doc = await JsonDocument.ParseAsync(memoryStream, JsonRpcConvertSettings.DocumentOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new ParseErrorException(ex.Message);
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.EnumerateArray().Select(DecodeResponse).ToArray();
                }

                return new[] { DecodeResponse(doc.RootElement) };

            }
            finally
            {
                doc?.Dispose();
            }
        }


        /// <summary>
        /// Convert stream to JsonRpcResponse array.
        /// </summary>
        /// <param name="stream">The stream to be converted.</param>
        /// <returns>The JsonRpcResponse array converted from stream.</returns>
        public static async Task<JsonRpcResponse[]> DecodeResponsesAsync(Stream stream)
        {
            JsonDocument doc = null;
            try
            {
                try
                {
                    doc = await JsonDocument.ParseAsync(stream, JsonRpcConvertSettings.DocumentOptions).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new ParseErrorException(ex.Message);
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return doc.RootElement.EnumerateArray().Select(DecodeResponse).ToArray();
                }

                return new[] { DecodeResponse(doc.RootElement) };

            }
            finally
            {
                doc?.Dispose();
            }
        }



        /// <summary>
        /// Convert param json string to JsonRpcArguments.
        /// </summary>
        /// <param name="paramString">The parameter(s)'s json string to be converted.</param>
        /// <param name="parameters">The parameters of the calling method.</param>
        /// <returns>The converted arguments.</returns>
        internal static async Task<object[]> DecodeArgumentsAsync(string paramString, IReadOnlyList<JsonRpcCallParameter> parameters)
        {
            if (parameters.Count == 0)
            {
                return Array.Empty<object>();
            }

            using var parameterData = Utf8StringData.Get(paramString);//new Utf8StringData(paramString);
            using var paramDoc = await JsonDocument.ParseAsync(parameterData.Stream, JsonRpcConvertSettings.DocumentOptions);
            if (parameters.Count == 1)
            {
                // paramString is an object
                var parameter = parameters[0];
                object parameterValue;
                try
                {
                    var parameterElement = paramDoc.RootElement.ValueKind == JsonValueKind.Array ? paramDoc.RootElement[0] : paramDoc.RootElement;
                    using var paramValueData = Utf8StringData.Get(parameterElement.GetRawText());//new Utf8StringData(parameterElement.GetRawText());
                    parameterValue = await JsonSerializer.DeserializeAsync(paramValueData.Stream, parameter.ParameterType, JsonRpcConvertSettings.SerializerOptions);
                }
                catch (Exception ex)
                {
                    throw new InvalidParamsException(ex.Message);
                }
                return new[] {parameterValue};
            }

            // paramString is an array
            var arrayLength = paramDoc.RootElement.GetArrayLength();
            var arguments = new object[arrayLength];
            var parameterElements = paramDoc.RootElement.EnumerateArray();
            var index = 0;
            foreach (var parameterElement in parameterElements)
            {
                var parameter = parameters[index];
                object parameterValue;
                try
                {
                    using var paramValueData = Utf8StringData.Get(parameterElement.GetRawText());//new Utf8StringData(parameterElement.GetRawText());
                    parameterValue = await JsonSerializer.DeserializeAsync(paramValueData.Stream, parameter.ParameterType, JsonRpcConvertSettings.SerializerOptions);
                }
                catch (Exception ex)
                {
                    throw new InvalidParamsException(ex.Message);
                }

                arguments[index] = parameterValue;
                index++;
            }
            return arguments;
        }


        /// <summary>
        /// Convert requests to binary data which in Json format.
        /// </summary>
        /// <param name="requests">The requests to convert.</param>
        /// <returns>The converted binary data which in Json format.</returns>
        public static async Task<byte[]> EncodeRequestsAsync(JsonRpcRequest[] requests)
        {
            await using var stream = new MemoryStream();
            switch (requests.Length)
            {
                case 0:
                    return null;
                case 1:
                {
                    var rentData = RequestDataPool.Rent();
                    try
                    {
                        JsonDocument doc = null;
                        try
                        {
                            var request = requests[0];
                            rentData.Id = request.Id;
                            rentData.Method = request.Method;
                            if (request.Params.Type == RequestParameterType.RawString)
                            {
                                using var utf8StringData = Utf8StringData.Get((string) request.Params.Value);
                                doc = await JsonDocument.ParseAsync(utf8StringData.Stream,JsonRpcConvertSettings.DocumentOptions);
                                rentData.Params = doc.RootElement;
                            }
                            else
                            {
                                rentData.Params = request.Params.Value;
                            }

                            await JsonSerializer.SerializeAsync(stream, rentData.GetData(),JsonRpcConvertSettings.SerializerOptions);
                        }
                        finally
                        {
                            doc?.Dispose();
                        }
                    }
                    finally
                    {
                        RequestDataPool.Return(rentData);
                    }
                    break;
                }
                default:
                    {
                        var requestDataArray = new Dictionary<string, object>[requests.Length];
                        var rentDataArray = new JsonRpcRequestData[requests.Length];
                        try
                        {
                            var jsonDocumentList = new List<JsonDocument>();
                            try
                            {
                                for (var i = 0; i < requests.Length; i++)
                                {
                                    var request = requests[i];
                                    var rentData = RequestDataPool.Rent();
                                    rentData.Id = request.Id;
                                    rentData.Method = request.Method;
                                    if (request.Params.Type == RequestParameterType.RawString)
                                    {
                                        using var utf8StringData = Utf8StringData.Get((string) request.Params.Value);
                                        var doc = await JsonDocument.ParseAsync(utf8StringData.Stream,JsonRpcConvertSettings.DocumentOptions);
                                        rentData.Params = doc.RootElement;
                                        jsonDocumentList.Add(doc);
                                    }
                                    else
                                    {
                                        rentData.Params = request.Params.Value;
                                    }
                                    requestDataArray[i] = rentData.GetData();
                                    rentDataArray[i] = rentData;
                                }

                                await JsonSerializer.SerializeAsync(stream, requestDataArray,JsonRpcConvertSettings.SerializerOptions);
                            }
                            finally
                            {
                                foreach (var jsonDocument in jsonDocumentList)
                                {
                                    jsonDocument.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            foreach (var rentData in rentDataArray)
                            {
                                RequestDataPool.Return(rentData);
                            }
                        }

                        break;
                    }
            }
            return stream.ToArray();
        }


        /// <summary>
        /// Convert responses to binary data which in Json format.
        /// </summary>
        /// <param name="responses">The responses to convert.</param>
        /// <returns>The converted binary data which in Json format.</returns>
        public static async Task<byte[]> EncodeResponsesAsync(JsonRpcResponse[] responses)
        {
            await using var stream = new MemoryStream();
            switch (responses.Length)
            {
                case 0:
                    return null;
                case 1:
                {
                    var rentData = ResponseDataPool.Rent();
                    try
                    {
                        var response = responses[0];
                        rentData.Id = response.Id;
                        rentData.Data = response.Result;
                        await JsonSerializer.SerializeAsync(stream, rentData.GetData(),JsonRpcConvertSettings.SerializerOptions);
                    }
                    finally
                    {
                        ResponseDataPool.Return(rentData);
                    }

                    break;
                }
                default:
                {
                    var responseDataArray = new Dictionary<string, object>[responses.Length];
                    var rentDataArray = new JsonRpcResponseData[responses.Length];
                    try
                    {
                        for (var i = 0; i < responses.Length; i++)
                        {
                            var response = responses[i];
                            var responseData = ResponseDataPool.Rent();
                            responseData.Id = response.Id;
                            responseData.Data = response.Result;
                            responseDataArray[i] = responseData.GetData();
                            rentDataArray[i] = responseData;
                        }
                        await JsonSerializer.SerializeAsync(stream, responseDataArray, JsonRpcConvertSettings.SerializerOptions);
                    }
                    finally
                    {
                        foreach (var rentData in rentDataArray)
                        {
                            ResponseDataPool.Return(rentData);
                        }
                    }
                    break;
                }
            }
            return stream.ToArray();
        }

    }
}
