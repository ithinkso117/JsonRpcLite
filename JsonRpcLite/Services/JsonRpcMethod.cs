using System;

namespace JsonRpcLite.Services
{
    /// <summary>
    /// Delegate with result return..
    /// </summary>
    /// <param name="obj">The object reference.</param>
    /// <param name="arguments">The arguments pass to the call.</param>
    /// <returns>The return object.</returns>
    internal delegate object Invoke(object obj, object[] arguments);

    /// <summary>
    /// Delegate without result return..
    /// </summary>
    /// <param name="obj">The object reference.</param>
    /// <param name="arguments">The arguments pass to the call.</param>
    internal delegate void VoidInvoke(object obj, object[] arguments);


    internal abstract class JsonRpcMethod
    {
        /// <summary>
        /// Gets the method return type.
        /// </summary>
        public Type ReturnType { get; protected set; }

        public override string ToString()
        {
            var returnTypeStr = ReturnType == null ? "void" : ReturnType.Name;
            return $"{GetType().Name} - ReturnType:[{returnTypeStr}]";
        }
    }


    /// <summary>
    /// Default implementation for the IJsonRpcMethod
    /// </summary>
    internal class JsonRpcInvokeMethod : JsonRpcMethod
    {

        /// <summary>
        /// The method which to call the rpc method.
        /// </summary>
        private readonly Invoke _invoke;

        /// <summary>
        /// Create a CallMethod which implemented the ICallMethod
        /// </summary>
        /// <param name="invoke">The delegate to call the rpc method.</param>
        /// <param name="returnType">The return type of the method.</param>
        public JsonRpcInvokeMethod(Invoke invoke, Type returnType)
        {
            _invoke = invoke;
            ReturnType = returnType;
        }

        /// <summary>
        /// Call the Rpc method implementation
        /// </summary>
        /// <param name="thisObject">The this object which will pass to the method for the first argument</param>
        /// <param name="args">The arguments for the rpc method.</param>
        /// <returns>The return value of the method.</returns>
        public object Invoke(object thisObject, object[] args)
        {
           return _invoke(thisObject, args);
        }
    }


    internal class JsonRpcVoidInvokeMethod : JsonRpcMethod
    {

        /// <summary>
        /// The method which to call the rpc method.
        /// </summary>
        private readonly VoidInvoke _invoke;


        /// <summary>
        /// Create a VoidCallMethod which implemented the IVoidCallMethod
        /// </summary>
        /// <param name="invoke">The delegate to call the rpc method.</param>
        public JsonRpcVoidInvokeMethod(VoidInvoke invoke)
        {
            _invoke = invoke;
            ReturnType = null;
        }


        /// <summary>
        /// Call the Rpc method implementation
        /// </summary>
        /// <param name="thisObject">The this object which will pass to the method for the first argument</param>
        /// <param name="args">The arguments for the rpc method.</param>
        public void Invoke(object thisObject, object[] args)
        {
            _invoke(thisObject, args);
        }
    }

}
