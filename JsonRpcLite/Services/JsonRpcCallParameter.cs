using System;

namespace JsonRpcLite.Services
{
    /// <summary>
    /// The parameter information for the calling method.
    /// </summary>
    internal class JsonRpcCallParameter
    {
        /// <summary>
        /// Gets the parameter's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter's type.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Create the CallingParameter.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        public JsonRpcCallParameter(string name, Type parameterType)
        {
            Name = name;
            ParameterType = parameterType;
        }
    }
}
