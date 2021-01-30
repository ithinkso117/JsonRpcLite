using System;
using System.Collections.Generic;
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
