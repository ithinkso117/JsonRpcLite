using System;

namespace JsonRpcLite.Network
{
    internal class HttpException:Exception
    {
        /// <summary>
        /// Gets the error code of the rpc call.
        /// </summary>
        public int ErrorCode { get; }


        public HttpException(int errorCode, string message):base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
