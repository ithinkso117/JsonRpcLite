using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonRpcLite.Services
{

    internal interface ISmdType
    {
        /// <summary>
        /// Get the name of the SmdType.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Get the type of the SmdType.
        /// </summary>
        object Type { get; set; }
    }


    internal interface ISmdParameter
    {
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        ISmdType Type { get; set; }

    }


    internal interface ISmdMethod
    {
        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the method.
        /// </summary>
        ISmdParameter[] Parameters { get; set; }

        /// <summary>
        /// Gets or sets the return type of the method.
        /// </summary>
        ISmdType Returns { get; set; }

    }

    internal class SmdService
    {
        private class SmdBase
        {
            /// <summary>
            /// Gets the target for the base smd object, for checking the relationship of the service.
            /// </summary>
            public string Target { get; }

            protected SmdBase(string target)
            {
                Target = target;
            }
        }

        private class TypeId
        {
            private readonly int _value;

            public TypeId(int value)
            {
                _value = value;
            }

            /// <summary>
            /// Get the id value as Int.
            /// </summary>
            /// <returns>The int value of the id</returns>
            public int AsInt()
            {
                return _value;
            }

            /// <summary>
            /// Gets the id value as string.
            /// </summary>
            /// <returns>The string value of the id.</returns>
            public string AsString()
            {
                return _value.ToString();
            }
        }

        private class SmdType : SmdBase, ISmdType
        {
            /// <summary>
            /// Gets or sets the Id of the type.
            /// </summary>
            public TypeId Id { get; set; }

            /// <summary>
            /// Get the name of the SmdType.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Get the type of the SmdType.
            /// </summary>
            public object Type { get; set; }

            protected SmdType(string target) : base(target)
            {
            }

            /// <summary>
            /// Get the data which will be serialized.
            /// </summary>
            /// <returns>The data of the SmdType</returns>
            public virtual Dictionary<string, object> ToTypeData()
            {
                throw new NotSupportedException();
            }

            public override string ToString()
            {
                return $"{Name} - {GetHashCode()}";
            }
        }


        private class SimpleSmdType : SmdType
        {
            public SimpleSmdType(string target) : base(target)
            {
            }

            public override Dictionary<string, object> ToTypeData()
            {
                string jsonType;
                switch ((string)Type)
                {
                    case "sbyte":
                    case "byte":
                    case "int16":
                    case "uint16":
                    case "int32":
                    case "uint32":
                    case "int64":
                    case "uint64":
                    case "single":
                    case "double":
                    case "decimal":
                        jsonType = "number";
                        break;
                    
                    case "boolean":
                        jsonType = "boolean";
                        break;
                    case "char":
                    case "string":
                    case "datetime":
                        jsonType = "string";
                        break;
                    default:
                        jsonType = "string";
                        break;
                }
                var data = new Dictionary<string, object>
                {
                    {"name", Name},
                    {"type", jsonType},
                };
                return data;
            }
        }

        private class ArraySmdType : SmdType
        {
            public ArraySmdType(string target) : base(target)
            {
            }

            public override Dictionary<string, object> ToTypeData()
            {
                var data = new Dictionary<string, object>
                {
                    {"name", Name},
                    {"type", $"array#{((SmdType)Type).Id.AsInt()}"},
                };
                return data;
            }
        }

        private class ComplexSmdType : SmdType
        {
            public ComplexSmdType(string target) : base(target)
            {
            }

            public override Dictionary<string, object> ToTypeData()
            {
                var properties = new Dictionary<string, object>();
                if (Type is Dictionary<string, ISmdType> smdProperties)
                {
                    foreach (var name in smdProperties.Keys)
                    {
                        var smdType = (SmdType)smdProperties[name];
                        properties.Add(name, smdType.Id.AsInt());
                    }
                }
                var data = new Dictionary<string, object>
                {
                    {"name", Name},
                    {"type", properties},
                };
                return data;
            }
        }


        private class SmdParameter : SmdBase, ISmdParameter
        {
            /// <summary>
            /// Gets or sets the name of the parameter.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the name of the type.
            /// </summary>
            public ISmdType Type { get; set; }


            public SmdParameter(string target) : base(target)
            {
            }

            /// <summary>
            /// Get the data which will be serialized.
            /// </summary>
            /// <returns>The data of the SmdParameter</returns>
            public Dictionary<string, object> ToParameterData()
            {
                var data = new Dictionary<string, object>
                {
                    {"name", Name},
                    {"type", ((SmdType)Type).Id.AsInt()},
                };
                return data;
            }
        }


        private class SmdMethod : SmdBase, ISmdMethod
        {

            /// <summary>
            /// Gets or sets the method name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the parameters of the method.
            /// </summary>
            public ISmdParameter[] Parameters { get; set; }

            /// <summary>
            /// Gets or sets the return type of the method.
            /// </summary>
            public ISmdType Returns { get; set; }

            public SmdMethod(string target) : base(target)
            {
            }

            /// <summary>
            /// Get the data which will be serialized.
            /// </summary>
            /// <returns>The data of the SmdMethod</returns>
            public Dictionary<string, object> ToMethodData()
            {
                var parametersData = new List<Dictionary<string, object>>();
                foreach (var parameter in Parameters)
                {
                    parametersData.Add(((SmdParameter)parameter).ToParameterData());
                }
                var data = new Dictionary<string, object>
                {
                    {"transport", "POST"},
                    {"envelope", "JSON-RPC-2.0"},
                    {"parameters", parametersData},
                };
                if (Returns != null)
                {
                    data.Add("returns", ((SmdType)Returns).Id.AsInt());
                }
                return data;
            }
        }


        private readonly JsonSerializerOptions _options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            IgnoreNullValues = true,
        };

        private readonly List<SmdType> _types = new();
        private readonly List<SmdMethod> _methods = new();


        /// <summary>
        /// Gets or sets the target of the method.
        /// </summary>
        public string Target { get;}


        public SmdService(string target)
        {
            Target = target;
        }


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
                    || name == "system.string"
                    || name == "system.datetime"
                )
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Create a ISmdType for this service.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <returns>The created ISmdType.</returns>
        public ISmdType CreateSmdType(Type type)
        {
            var smdType = _types.FirstOrDefault(x => x.Name == type.Name);
            if (smdType != null)
            {
                return smdType;
            }

            //For supporting Task<T>
            if (typeof(Task).IsAssignableFrom(type) && type.IsGenericType)
            {
                return CreateSmdType(type.GetGenericArguments().Single());
            }

            if (IsSimpleType(type))
            {
                var simpleSmdType = new SimpleSmdType(Target)
                {
                    Name = type.Name,
                    Type = type.Name.ToLower()
                };
                AddSimpleType(simpleSmdType);
                return simpleSmdType;
            }

            if (type.IsArray)
            {
                var arraySmdType = new ArraySmdType(Target)
                {
                    Name = type.Name,
                    Type = CreateSmdType(type.GetElementType())
                };
                AddArrayType(arraySmdType);
                return arraySmdType;
            }

            var properties = type.GetProperties().Where(x => x.GetAccessors().Any(y => y.IsPublic)).ToArray();
            var propertyTypes = new Dictionary<string, ISmdType>();
            foreach (var item in properties)
            {
                propertyTypes.Add(item.Name, CreateSmdType(item.PropertyType));
            }

            var complexSmdType = new ComplexSmdType(Target) { Name = type.Name, Type = propertyTypes };
            AddComplexType(complexSmdType);
            return complexSmdType;
        }


        private void AddSimpleType(SimpleSmdType type)
        {
            if (_types.All(x => x.Name != type.Name))
            {
                var lastSimpleTypeIndex = _types.FindLastIndex(x => x is SimpleSmdType);
                var typeIndex = lastSimpleTypeIndex + 1;
                _types.Insert(typeIndex, type);
            }
        }

        private void AddArrayType(ArraySmdType type)
        {
            if (_types.All(x => x.Name != type.Name))
            {
                _types.Add(type);
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
        /// Create a ISmdParameter for this service.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the parameter.</param>
        /// <returns>The created ISmdParameter</returns>
        public ISmdParameter CreateSmdParameter(string name, ISmdType type)
        {
            if (!(type is SmdType smdType))
            {
                throw new InvalidOperationException($"Invalided argument \"{type}\".");
            }

            if (smdType.Target != Target)
            {
                throw new InvalidOperationException($"Argument \"{type}\" is not created by this service.");
            }
            return new SmdParameter(Target) { Name = name, Type = type };
        }

        /// <summary>
        /// Create a ISmdMethod for this service.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="returns"></param>
        /// <returns></returns>
        public ISmdMethod CreateSmdMethod(string name, ISmdParameter[] parameters, ISmdType returns)
        {
            foreach (var parameter in parameters)
            {
                if (!(parameter is SmdParameter smdParameter))
                {
                    throw new InvalidOperationException($"Invalided argument \"{parameters}\".");
                }
                if (smdParameter.Target != Target)
                {
                    throw new InvalidOperationException($"Argument \"{parameters}\" is not created by this service.");
                }
            }

            if (returns != null)
            {
                if (!(returns is SmdType smdType))
                {
                    throw new InvalidOperationException($"Invalided argument \"{returns}\".");
                }
                if (smdType.Target != Target)
                {
                    throw new InvalidOperationException($"Argument \"{returns}\" is not created by this service.");
                }
            }

            return new SmdMethod(Target) { Name = name, Parameters = parameters, Returns = returns };
        }


        /// <summary>
        /// Add one method into the SmdService.
        /// </summary>
        /// <param name="method">The method to add.</param>
        public void AddMethod(ISmdMethod method)
        {
            if (!(method is SmdMethod smdMethod))
            {
                throw new InvalidOperationException($"Invalided argument \"{method}\".");
            }
            if (smdMethod.Target != Target)
            {
                throw new InvalidOperationException($"Argument \"{smdMethod}\" is not created by this service.");
            }
            foreach (var existMethod in _methods)
            {
                if (existMethod.Name == smdMethod.Name)
                {
                    throw new InvalidOperationException("Method name already exists.");
                }
            }

            _methods.Add(smdMethod);
        }


        public Dictionary<string, object> ToServiceData()
        {
            var types = new Dictionary<string, Dictionary<string, object>>();
            for (var i = 0; i < _types.Count; i++)
            {
                var type = _types[i];
                type.Id = new TypeId(i);
            }

            foreach (var type in _types)
            {
                types.Add(type.Id.AsString(), type.ToTypeData());
            }
            var services = new Dictionary<string, Dictionary<string, object>>();
            foreach (var method in _methods)
            {
                services.Add(method.Name, method.ToMethodData());
            }
            var data = new Dictionary<string, object>
            {
                {"transport", "GET"},
                {"envelope", "URL"},
                {"target", Target},
                {"types", types},
                {"services", services},
            };
            return data;
        }

        /// <summary>
        /// Convert service to json data.
        /// </summary>
        /// <returns>The json data.</returns>
        public async Task<byte[]> ToUtf8JsonAsync()
        {
            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, ToServiceData(), _options);
            return stream.ToArray();
        }

    }
}
