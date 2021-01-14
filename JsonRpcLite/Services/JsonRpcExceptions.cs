using System;

namespace JsonRpcLite.Services
{
    internal class RpcException : Exception
    {
        /// <summary>
        /// Gets the error code of the rpc call.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Gets the internal message of the exception.
        /// </summary>
        public string InternalMessage { get; }

        public RpcException(int errorCode, string message, string internalMessage):base(message)
        {
            ErrorCode = errorCode;
            InternalMessage = internalMessage;
        }
    }

    /// <summary>
    /// Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text.
    /// </summary>
    internal class ParseErrorException : RpcException
    {
        public ParseErrorException(string internalMessage = "") : base(-32700,
            "Invalid JSON was received by the server. An error occurred on the server while parsing the JSON text.",
            internalMessage)
        {
        }
    }

    /// <summary>
    /// The JSON sent is not a valid Request object.
    /// </summary>
    internal class InvalidRequestException : RpcException
    {
        public InvalidRequestException(string internalMessage = "") : base(-32600,
            "The JSON sent is not a valid Request object.", internalMessage)
        {
        }
    }

    /// <summary>
    /// The method does not exist / is not available.
    /// </summary>
    internal class MethodNotFoundException : RpcException
    {
        public MethodNotFoundException(string internalMessage = "") : base(-32601,
            "The method does not exist / is not available.", internalMessage)
        {
        }
    }

    /// <summary>
    /// Invalid method parameter(s).
    /// </summary>
    internal class InvalidParamsException : RpcException
    {
        public InvalidParamsException(string internalMessage = "") : base(-32602, "Invalid method parameter(s).",
            internalMessage)
        {
        }
    }

    /// <summary>
    /// Internal JSON-RPC error.
    /// </summary>
    internal class InternalErrorException : RpcException
    {
        public InternalErrorException(string internalMessage = "") : base(-32603, "Internal JSON-RPC error.",
            internalMessage)
        {
        }
    }

    /// <summary>
    /// Server errors.
    /// </summary>
    internal class ServerErrorException : RpcException
    {
        private static int _defaultServerErrorCode = -32000;

        /// <summary>
        /// The default error code of the server error.
        /// </summary>
        public static int DefaultServerErrorCode => _defaultServerErrorCode;

        public ServerErrorException(string message, string internalMessage = "") : base(_defaultServerErrorCode, $"Server error: {message}", internalMessage)
        {
        }

        /// <summary>
        /// Set the default server error code.
        /// </summary>
        /// <param name="errorCode">The error code for the server error.</param>
        public static void SetServerErrorCode(int errorCode)
        {
            if (errorCode > -32000 || errorCode < -32099)
            {
                throw new InvalidOperationException("Invalid error code, server error code should from and including -32768 to -32000");
            }
            _defaultServerErrorCode = errorCode;
        }
    }
}
