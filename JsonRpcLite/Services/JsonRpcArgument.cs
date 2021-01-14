namespace JsonRpcLite.Services
{
    internal class JsonRpcArgument
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Create the JsonRpcArgument
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        internal JsonRpcArgument(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
