namespace JsonRpcLite.Network
{
    internal class JsonRpcServiceInfo
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether is a smd request
        /// </summary>
        public bool IsSmdRequest { get; }

        public JsonRpcServiceInfo(string name, bool isSmdRequest)
        {
            Name = name;
            IsSmdRequest = isSmdRequest;
        }
    }
}
