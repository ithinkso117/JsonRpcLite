namespace JsonRpcLite.Services
{
    public enum RequestParameterType
    {
        /// <summary>
        /// The parameter is a raw string, need to be deserialized
        /// </summary>
        RawString,
        /// <summary>
        /// The parameter is an object.
        /// </summary>
        Object
    }

    public class JsonRpcRequestParameter
    {
        /// <summary>
        /// Gets the parameter type.
        /// </summary>
        public RequestParameterType Type { get; } 

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object Value { get; }

        public JsonRpcRequestParameter(RequestParameterType type, object value)
        {
            Type = type;
            Value = value;
        }
    }

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
        public JsonRpcRequestParameter Params { get; protected set; }

        /// <summary>
        /// Gets whether this request is a notification.
        /// </summary>
        public bool IsNotification => Id == null;

        public JsonRpcRequest(object id, string method, JsonRpcRequestParameter @params)
        {
            Id = id;
            Method = method;
            Params = @params;
        }
    }
}
