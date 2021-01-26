namespace JsonRpcLite.Network
{
    internal class JsonRpcServiceInfo
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string Name { get; }

        public JsonRpcServiceInfo(string name)
        {
            Name = name;
        }
    }
}
