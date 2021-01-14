using System;
using JsonRpcLite.Services;

namespace JsonRpcLite.Network
{
    internal class JsonRpcExceptionResponse:JsonRpcResponse
    {
        public JsonRpcExceptionResponse() : base(null)
        {
        }

        /// <summary>
        /// Write an exception to this response, if the obj to write is not an exception,
        /// An InvalidOperationException will be raised.
        /// </summary>
        /// <param name="obj">The obj to write.</param>
        public override void WriteResult(object obj)
        {
            if (!(obj is Exception))
            {
                throw new InvalidOperationException("The argument must be an Exception for JsonRpcServerErrorResponse");
            }
            Result = obj;
        }
    }
}
