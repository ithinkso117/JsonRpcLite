using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonRpcLite.Services
{
    internal class SmdType
    {
        /// <summary>
        /// Get the name of the SmdType.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get the type of the SmdType.
        /// </summary>
        public object Type { get; set; }

    }


    internal class SmdParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        public string Type { get; set; } 
    }

    internal class SmdMethod
    {
        /// <summary>
        /// Gets or sets the transport of the method.
        /// </summary>
        public string Transport { get; set; } = "POST";

        /// <summary>
        /// Gets or sets the envelope of the method.
        /// </summary>
        public string Envelope { get; set; } = "JSON-RPC-2.0";

        /// <summary>
        /// Gets or sets the parameters of the method.
        /// </summary>
        public SmdParameter[] Parameters { get; set; }

        /// <summary>
        /// Gets or sets the returns of the method.
        /// </summary>
        public string Returns { get; set; }
    }

    internal class SmdService
    {
        private class SimpleSmdType : SmdType
        {

        }

        private class ComplexSmdType : SmdType
        {

        }


        private readonly JsonSerializerOptions _options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly List<SmdType> _types = new();
        private readonly Dictionary<string, SmdMethod> _methods = new();


        /// <summary>
        /// Gets or sets the transport of the method.
        /// </summary>
        public string Transport { get; set; } = "GET";

        /// <summary>
        /// Gets or sets the envelope of the method.
        /// </summary>
        public string Envelope { get; set; } = "URL";

        /// <summary>
        /// Gets or sets the target of the method.
        /// </summary>
        public string Target { get; set; }


        /// <summary>
        /// Gets the types used in this service.
        /// </summary>
        public Dictionary<string, SmdType> Types => _types.ToDictionary(type=> type.Name, type => type);

        /// <summary>
        /// The services which contains all methods.
        /// </summary>
        public Dictionary<string, SmdMethod> Services => _methods;


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

        public SmdType CreateSmdType(Type type)
        {
            //TODO support Generic types, otherwise need to check there is no Generic types in services.
            if (IsSimpleType(type))
            {
                var simpleSmdType = new SimpleSmdType{Name = type.Name, Type = type.Name.ToLower()};
                AddSimpleType(simpleSmdType);
                return simpleSmdType;
            }

            var properties = type.GetProperties().Where(x=>x.GetAccessors().Any(y=>y.IsPublic)).ToArray();
            var propertyTypes = new Dictionary<string, string>();
            foreach (var item in properties)
            {
                SmdType propertyType;
                if (IsSimpleType(item.PropertyType))
                {
                    var simpleSmdType = new SimpleSmdType{Name = item.PropertyType.Name, Type = item.PropertyType.Name.ToLower()};
                    AddSimpleType(simpleSmdType);
                    propertyType = simpleSmdType;
                }
                else
                {
                    propertyType = new SmdType { Name = item.PropertyType.Name, Type = CreateSmdType(item.PropertyType).Name };
                }
                propertyTypes.Add(item.Name, propertyType.Name);
            }
            var complexSmdType = new ComplexSmdType { Name = type.Name, Type = propertyTypes };
            AddComplexType(complexSmdType);
            return complexSmdType;
        }


        private void AddSimpleType(SimpleSmdType type)
        {
            if (_types.All(x => x.Name != type.Name))
            {
                _types.Insert(0,type);
            }
        }


        private void AddComplexType(ComplexSmdType type)
        {
            if (_types.All(x => x.Name != type.Name))
            {
                _types.Add(type);
            }
        }


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
            await JsonSerializer.SerializeAsync(stream, this, _options);
            return stream.GetBuffer();
        }

        /// <summary>
        /// Gets the json string of this service.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, _options);
        }
    }
}
