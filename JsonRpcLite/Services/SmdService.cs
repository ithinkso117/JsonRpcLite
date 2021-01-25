using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JsonRpcLite.Services
{
    internal class SmdType
    {
        /// <summary>
        /// Get the name of the SmdType, this property is only used when the SmdType is a parameter type.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Get the type of the SmdType.
        /// </summary>
        [JsonPropertyName("type")]
        public object Type { get; set; }

        /// <summary>
        /// Check if the type is a simple type.
        /// </summary>
        /// <param name="t">The type to check</param>
        /// <returns>If it is a simple type it should be true otherwise is false</returns>
        private static bool IsSimpleType(Type t)
        {
            if (t.FullName != null)
            {
                var name = t.FullName.ToLower();

                if (name == "system.sbyte"
                    || name == "system.byte"
                    || name == "system.int16"
                    || name == "system.uint16"
                    || name == "system.int32"
                    || name == "system.uint32"
                    || name == "system.int64"
                    || name == "system.uint64"
                    || name == "system.char"
                    || name == "system.single"
                    || name == "system.double"
                    || name == "system.boolean"
                    || name == "system.decimal"
                    || name == "system.float"
                    || name == "system.numeric"
                    || name == "system.money"
                    || name == "system.string"
                    || name == "system.object"
                    || name == "system.type"
                    // || name == "system.datetime"
                    )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create a smd type for return type.
        /// </summary>
        /// <param name="type">The type to be created.</param>
        /// <returns>The created SmdType.</returns>
        public static SmdType CreateReturnType(Type type)
        {
            return Create(null, type);
        }

        /// <summary>
        /// Create a smd type for parameter type.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <returns>The created SmdType.</returns>
        public static SmdType CreateParameterType(string name, Type type)
        {
            return Create(name, type);
        }


        /// <summary>
        /// Create a SmdType by given name and type.
        /// </summary>
        /// <param name="name">The name of the smd type, it should be null when it is a return type.</param>
        /// <param name="type">The type of the smd type.</param>
        /// <returns></returns>
        private static SmdType Create(string name, Type type)
        {
            //TODO support Generic types, otherwise need to check there is no Generic types in services.
            if (IsSimpleType(type))
            {
                return new SmdType { Name = name, Type = type.Name.ToLower() };
            }

            var properties = type.GetProperties();
            var types = new Dictionary<string, SmdType>();
            foreach (var item in properties)
            {
                if (item.GetAccessors().Any(x => x.IsPublic))
                {
                    //Child property can not be return type.
                    types.Add(item.Name, Create(null,item.PropertyType));
                }
            }
            return new SmdType {Name = name, Type = types };
        }
    }

    internal class SmdMethod
    {
        /// <summary>
        /// Gets or sets the transport of the method.
        /// </summary>
        [JsonPropertyName("transport")] public string Transport { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the envelope of the method.
        /// </summary>
        [JsonPropertyName("envelope")] public string Envelope { get; set; } = "JSON-RPC-2.0";


        /// <summary>
        /// Gets or sets the target of the method.
        /// </summary>
        [JsonPropertyName("target")] public string Target { get; set; }


        /// <summary>
        /// Gets or sets the parameters of the method.
        /// </summary>
        [JsonPropertyName("parameters")] public SmdType[] Parameters { get; set; }


        /// <summary>
        /// Gets or sets the returns of the method.
        /// </summary>
        [JsonPropertyName("returns")] public SmdType Returns { get; set; }
    }

    internal class SmdService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, 
            WriteIndented = true, 
            IgnoreNullValues = true
        };

        private readonly Dictionary<string, SmdMethod> _methods = new();

        /// <summary>
        /// The services which contains all methods.
        /// </summary>
        [JsonPropertyName("services")]
        public Dictionary<string, SmdMethod> Services => _methods;

        /// <summary>
        /// Add one method into the SmdService.
        /// </summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="method">The method to add.</param>
        public void AddMethod(string name, SmdMethod method)
        {
            _methods.Add(name, method);
        }

        /// <summary>
        /// Convert service to json data.
        /// </summary>
        /// <returns>The json data.</returns>
        public async Task<byte[]> ToUtf8JsonAsync()
        {
            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, this, Options);
            return stream.GetBuffer();
        }

        /// <summary>
        /// Gets the json string of this service.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, Options);
        }
    }
}
