using System;
using System.Linq;
using System.Threading.Tasks;

namespace JsonRpcLite.Services
{
    internal class JsonRpcTypeChecker
    {
        /// <summary>
        /// Gets or sets the max depth to check the child types.
        /// </summary>
        public int MaxDepth { get; set; } = 64;


        /// <summary>
        /// Gets the error message while type is not allowed.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Check whether the type is allowed.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if allow otherwise not.</returns>
        public bool IsReturnTypeAllowed(Type type)
        {
            //Special case.
            if (typeof(DateTime) == type)
            {
                return true;
            }
            if (typeof(Task).IsAssignableFrom(type))
            {
                return true;
            }
            var level = 0;
            return InternalIsTypeAllowed(type, ref level);
        }

        /// <summary>
        /// Check whether the type is allowed.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if allow otherwise not.</returns>
        public bool IsParameterTypeAllowed(Type type)
        {
            //Special case.
            if (typeof(DateTime) == type)
            {
                return true;
            }
            var level = 0;
            return InternalIsTypeAllowed(type, ref level);
        }

        private bool InternalIsTypeAllowed(Type type, ref int level)
        {
            level++;
            if (level > MaxDepth)
            {
                Error = "Over_MaxDepth";
                return false;
            }
            if (type == typeof(object))
            {
                Error = "NotSupport_BaseObject";
                return false;
            }

            if (type.IsByRef)
            {
                Error = "NotSupport_ByRef";
                return false;
            }

            //WE DO NOT support generic type.
            if (type.IsGenericType)
            {
                Error = "NotSupport_GenericType";
                return false;
            }

            if (type.IsArray)
            {
                return InternalIsTypeAllowed(type.GetElementType(), ref level);
            }

            var properties = type.GetProperties().Where(x => x.GetAccessors().Any(y => y.IsPublic)).ToArray();
            foreach (var item in properties)
            {
                if (!InternalIsTypeAllowed(item.PropertyType, ref level))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
