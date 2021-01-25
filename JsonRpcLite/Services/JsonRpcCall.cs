using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JsonRpcLite.Utilities;

namespace JsonRpcLite.Services
{
    internal class JsonRpcCall
    {
        /// <summary>
        /// The invoker to call.
        /// </summary>
        private readonly JsonRpcMethod _method;

        /// <summary>
        /// The this object for call, the class method need the this object for the first argument.
        /// </summary>
        private readonly object _thisObject;

        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the information for the parameters. 
        /// </summary>
        public IReadOnlyList<JsonRpcCallParameter> Parameters { get; }


        /// <summary>
        /// Gets the method's return type.
        /// </summary>
        public Type ReturnType => _method.ReturnType;


        /// <summary>
        /// Create the JsonRpcCall.
        /// </summary>
        /// <param name="thisObject">The object which contains the rpc Method.</param>
        /// <param name="name">The name of the method.</param>
        /// <param name="parameters">The parameters information of the method.</param>
        /// <param name="method">The rpc method to call internal method.</param>
        public JsonRpcCall(object thisObject, string name, JsonRpcCallParameter[] parameters, JsonRpcMethod method)
        {
            _thisObject = thisObject;
            Name = name;
            Parameters = parameters;
            _method = method;
        }

        /// <summary>
        /// Call the rpc method.
        /// </summary>
        /// <param name="arguments">The arguments which will pass to the method.</param>
        /// <returns>The result from the call.</returns>
        public async Task<object> Call(object[] arguments)
        {
            switch (_method)
            {
                case JsonRpcVoidInvokeMethod jsonRpcVoidInvokeMethod:
                    jsonRpcVoidInvokeMethod.Invoke(_thisObject, arguments);
                    return null;
                case JsonRpcInvokeMethod jsonRpcInvokeMethod:
                {
                    var result = jsonRpcInvokeMethod.Invoke(_thisObject, arguments);
                    if (result is Task task)
                    {
                        return await TaskResult.Get(task).ConfigureAwait(false);
                    }
                    return result;
                }
                default:
                    return null;
            }
        }
    }

}
