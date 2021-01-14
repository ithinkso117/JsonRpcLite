namespace JsonRpcLite.Services
{
    public class JsonRpcResponse
    {
        /// <summary>
        /// Gets the Id of the response.
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Gets result of this response.
        /// </summary>
        public object Result { get; protected set; }


        internal JsonRpcResponse(object id = null)
        {
            Id = id;
        }

        /// <summary>
        /// Write the result object.
        /// </summary>
        /// <param name="obj">The object to write back</param>
        public virtual void WriteResult(object obj)
        {
            Result = obj;
        }

    }
}
