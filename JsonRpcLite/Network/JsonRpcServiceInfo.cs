namespace JsonRpcLite.Network
{
    internal class JsonRpcServiceInfo
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version of the service. 
        /// </summary>
        public string Version { get; }

        public JsonRpcServiceInfo(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
