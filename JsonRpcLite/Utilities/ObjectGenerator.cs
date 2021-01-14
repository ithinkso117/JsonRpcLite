using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace JsonRpcLite.Utilities
{
    internal delegate object CreateObject();
    internal delegate object SetParameters(object obj, object[] parameters);

    internal class PropertySetter
    {
        /// <summary>
        /// Gets the object type of the setter.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the properties of the setter which can be set.
        /// </summary>
        public PropertyInfo[] Properties { get; }

        /// <summary>
        /// Gets the methods which used to set the parameters.
        /// </summary>
        public SetParameters SetParameters { get; }

        public PropertySetter(Type type, PropertyInfo[] properties, SetParameters setParameters)
        {
            Type = type;
            Properties = properties;
            SetParameters = setParameters;
        }

    }

    internal static class ObjectGenerator
    {

        private static readonly Type[] CreatorParameters = Array.Empty<Type>();
        private static readonly Dictionary<Type, CreateObject> Creators = new();
        private static readonly Dictionary<Type, PropertySetter> Setters = new();

        /// <summary>
        /// Create new object by using Emit, it will be much faster than the reflection.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <returns>The created object.</returns>
        public static object New(this Type type)
        {
            if (!Creators.ContainsKey(type))
            {
                var defaultConstructor = type.GetConstructor(CreatorParameters);
                if (defaultConstructor == null)
                {
                    throw new InvalidOperationException($"No default constructor for type:{type}");
                }
                var dynamicMethod = new DynamicMethod( $"Create_{type.Name}",typeof(object),null);              
                var il = dynamicMethod.GetILGenerator();
                il.Emit(OpCodes.Newobj, defaultConstructor);
                il.Emit(OpCodes.Ret);
                var creator = (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
                Creators.Add(type, creator);
            }
            return Creators[type]();
        }


        /// <summary>
        /// Create new object by using Emit, it will be much faster than the reflection.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="parameters">The key value of the parameters, they will be converted according to the types.</param>
        /// <returns>The created object.</returns>
        public static object New(this Type type, NameValueCollection parameters)
        {
            if(!Setters.TryGetValue(type, out var setter))
            {
                var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite).ToArray();
                var setMethods = properties.Select(x=> type.GetMethod($"set_{x.Name}")).ToArray();

                var dynamicMethod = new DynamicMethod(
                    name: $"Set_{type.Name}_Parameters",
                    returnType: null,
                    parameterTypes: new[] { typeof(object), typeof(object[]) });

                var methodIl = dynamicMethod.GetILGenerator();

                for (var i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    var setMethod = setMethods[i];

                    methodIl.Emit(OpCodes.Ldarg_0);
                    methodIl.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
                    methodIl.Emit(OpCodes.Ldarg_1);
                    //Put an integer value in stack which is the index in object[]
                    methodIl.Emit(OpCodes.Ldc_I4_S, i);
                    //Get the reference of index which is the integer value from the object[].
                    methodIl.Emit(OpCodes.Ldelem_Ref);
                    //Cast or unbox the reference.
                    methodIl.Emit(property.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, property.PropertyType);
                    methodIl.Emit(OpCodes.Callvirt, setMethod);
                }
                methodIl.Emit(OpCodes.Ret);
                var setParameters = (SetParameters)dynamicMethod.CreateDelegate(typeof(SetParameters));
                setter = new PropertySetter(type, properties, setParameters);
                Setters.Add(type, setter);
            }
            var obj = type.New();
            var args = new List<object>();
            foreach (var property in setter.Properties)
            {
                if (parameters.AllKeys.Contains(property.Name))
                {
                    var propertyValue = Convert.ChangeType(parameters[property.Name], property.PropertyType);
                    if (propertyValue != null && property.PropertyType == typeof(string))
                    {
                        propertyValue = Uri.UnescapeDataString((string)propertyValue);
                    }
                    args.Add(propertyValue);
                }
                else
                {
                    args.Add(property.PropertyType.Default());
                }
            }
            setter.SetParameters(obj, args.ToArray());
            return obj;
        }


        /// <summary>
        /// Get the default value for value type, null for reference type.
        /// </summary>
        /// <param name="type">The type for the value.</param>
        /// <returns>The default value.</returns>
        public static object Default(this Type type)
        {
            if (!type.IsValueType) return null;
            if (type == typeof(byte))
            {
                return default(byte);
            }
            if (type == typeof(sbyte))
            {
                return default(sbyte);
            }
            if (type == typeof(short))
            {
                return default(short);
            }
            if (type == typeof(int))
            {
                return default(int);
            }
            if (type == typeof(float))
            {
                return default(float);
            }
            if (type == typeof(long))
            {
                return default(long);
            }
            if (type == typeof(double))
            {
                return default(double);
            }
            if (type == typeof(ushort))
            {
                return default(ushort);
            }
            if (type == typeof(uint))
            {
                return default(uint);
            }
            if (type == typeof(ulong))
            {
                return default(ulong);
            }
            if (type == typeof(char))
            {
                return default(char);
            }
            if (type == typeof(decimal))
            {
                return default(decimal);
            }
            if (type == typeof(bool))
            {
                return default(bool);
            }
            if (type.IsEnum)
            {
                return type.GetEnumValues().GetValue(0);
            }
            return type.New();
        }
    }
}
