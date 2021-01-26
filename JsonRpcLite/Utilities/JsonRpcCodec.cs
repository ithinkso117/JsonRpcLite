using System;
using JsonRpcLite.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonRpcLite.Utilities
{
    internal class JsonRpcCodec
    {
        private static readonly ObjectPool<JsonRpcResultData> ResultDataPool = new(() => new JsonRpcResultData());


        /// <summary>
        /// Convert one JsonElement to a JsonRpcHttpRequest
        /// </summary>
        /// <param name="element">The JsonElement to convert.</param>
        /// <returns>The converted JsonRpcHttpRequest</returns>
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
            return new JsonRpcRequest(id, method, parameters);
        }

        /// <summary>
        /// Convert binary data to JsonRpcHttpRequest array.
        /// </summary>
        /// <param name="requestData">The data to be converted.</param>
        /// <param name="dataLength">The available data length of the request data. if 0 use all data.</param>
        /// <returns>The JsonRpcHttpRequest array converted from request data.</returns>
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
                    doc = await JsonDocument.ParseAsync(memoryStream, JsonRpcConvertSettings.DocumentOptions);
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
                    doc = await JsonDocument.ParseAsync(stream, JsonRpcConvertSettings.DocumentOptions);
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
        /// Convert param json string to JsonRpcArguments.
        /// </summary>
        /// <param name="paramString">The parameter(s)'s json string to be converted.</param>
        /// <param name="parameters">The parameters of the calling method.</param>
        /// <returns>The converted arguments.</returns>
        public static async Task<object[]> DecodeArgumentsAsync(string paramString, IReadOnlyList<JsonRpcCallParameter> parameters)
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
                    var resultData = ResultDataPool.Get();
                    try
                    {
                        var response = responses[0];
                        resultData.Id = response.Id;
                        resultData.Data = response.Result;
                        await JsonSerializer.SerializeAsync(stream, resultData.GetData(),JsonRpcConvertSettings.SerializerOptions);
                    }
                    finally
                    {
                        ResultDataPool.Return(resultData);
                    }

                    break;
                }
                default:
                {
                    var responseDataArray = new Dictionary<string, object>[responses.Length];
                    var resultDataArray = new JsonRpcResultData[responses.Length];
                    try
                    {
                        for (var i = 0; i < responses.Length; i++)
                        {
                            var response = responses[i];
                            var resultData = ResultDataPool.Get();
                            resultData.Id = response.Id;
                            resultData.Data = response.Result;
                            responseDataArray[i] = resultData.GetData();
                            resultDataArray[i] = resultData;
                        }
                        await JsonSerializer.SerializeAsync(stream, responseDataArray, JsonRpcConvertSettings.SerializerOptions);
                    }
                    finally
                    {
                        foreach (var resultData in resultDataArray)
                        {
                            ResultDataPool.Return(resultData);
                        }
                    }
                    break;
                }
            }
            return stream.ToArray();
        }

    }
}
