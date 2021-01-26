using System;

namespace JsonRpcLite.Services
{
    /// <summary>
    /// Attribute for the service.
    /// </summary>
    public class RpcServiceAttribute : Attribute
    {
        /// <summary>
        /// Gets the Url string
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create attribute for given name.
        /// </summary>
        /// <param name="name">The service name</param>
        public RpcServiceAttribute(string name)
        {
            Name = name;
        }

        public RpcServiceAttribute()
        {
            Name = string.Empty;
        }
    }


    /// <summary>
    /// The JsonRpcMethod Attribute, means this method is a JsonRpc method
    /// </summary>
    public class RpcMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of this method.
        /// </summary>
        public string Name { get; }

        public RpcMethodAttribute()
        {
            Name = string.Empty;
        }

        public RpcMethodAttribute(string name)
        {
            Name = name;
        }
    }
}
