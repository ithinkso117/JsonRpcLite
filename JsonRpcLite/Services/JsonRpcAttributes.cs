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
        /// Gets the version of the handler.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Router to api handler with url string
        /// </summary>
        /// <param name="name">The url string</param>
        public RpcServiceAttribute(string name)
        {
            Version = "v1";
            Name = name;
        }

        /// <summary>
        /// Router to api handler with url string
        /// </summary>
        /// <param name="name">The url string</param>
        /// <param name="version">The api version</param>
        public RpcServiceAttribute(string name, string version)
        {
            Name = name;
            Version = version;
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
