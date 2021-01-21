using System;
using JsonRpcLite.Services;

namespace JsonRpcLite.Utilities
{
    internal static class ExceptionFormatter
    {
        /// <summary>
        /// Format the exception to string.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <returns>The formatted string.</returns>
        public static string Format(this Exception exception)
        {
            if (exception is RpcException rpcException)
            {
                return $"{rpcException.InternalMessage}{Environment.NewLine}{rpcException.StackTrace}";
            }

            return $"{exception.Message}{Environment.NewLine}{exception.StackTrace}";
        }
    }
}
