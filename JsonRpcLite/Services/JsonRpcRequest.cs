namespace JsonRpcLite.Services
{
    public class JsonRpcRequest
    {
        /// <summary>
        /// Gets the Id of the request.
        /// </summary>
        public object Id { get; protected set; }

        /// <summary>
        /// Gets the method name from the call.
        /// </summary>
        public string Method { get; protected set; }

        /// <summary>
        /// Gets the parameters json string content of the request.
        /// </summary>
        public string Params { get; protected set; }

        /// <summary>
        /// Gets whether this request is a notification.
        /// </summary>
        public bool IsNotification => Id == null;

        public JsonRpcRequest(object id, string method, string @params)
        {
            Id = id;
            Method = method;
            Params = @params;
        }

    }
}
