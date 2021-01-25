using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace JsonRpcLite.Services
{
    public class JsonRpcService
    {
        private readonly Dictionary<string, JsonRpcCall> _rpcCalls = new();

        /// <summary>
        /// Gets the service name.
        /// </summary>
        internal string Name { get; private set; }

        /// <summary>
        /// Gets the SmdData of this service.
        /// </summary>
        internal byte[] SmdData { get; private set; }


        public JsonRpcService()
        {
            var type = GetType();
            Name = $"{type.Name}";
            var serviceAttributes = type.GetCustomAttributes(typeof(RpcServiceAttribute), false);
            if (serviceAttributes.Length > 1)
            {
                throw new InvalidOperationException($"Service {Name} defined more than one rpc service attributes.");
            }
            if (serviceAttributes.Length > 0)
            {
                var serviceAttribute = (RpcServiceAttribute)serviceAttributes[0];
                if (!string.IsNullOrEmpty(serviceAttribute.Name))
                {
                    Name = $"{serviceAttribute.Name.ToLower()}";
                    if (!string.IsNullOrWhiteSpace(serviceAttribute.Version))
                    {
                        Name = $"{serviceAttribute.Name.ToLower()}/{serviceAttribute.Version}";
                    }
                }
            }
            RegisterCalls();
            GenerateSmd();
        }

        private void RegisterCalls()
        {
            //Load all methods.
            var methods = GetType().GetMethods();
            //Register available methods.
            var availableMethods = methods.Where(x => x.IsVirtual == false && x.IsStatic == false).ToArray();
            RegisterAvailableCalls(availableMethods);

        }


        /// <summary>
        /// Generate the smd data.
        /// </summary>
        private void GenerateSmd()
        {
            var smdService = new SmdService();
            foreach (var rpcCall in _rpcCalls.Values)
            {
                var method = new SmdMethod(); ;
                var methodName = rpcCall.Name;
                if (rpcCall.ReturnType != null)
                {
                    method.Returns = SmdType.CreateReturnType(rpcCall.ReturnType);
                }
                method.Target = Name;
                var parameters = new SmdType[rpcCall.Parameters.Count];
                var index = 0;
                foreach (var rpcCallParameter in rpcCall.Parameters)
                {
                    var smdParameter = SmdType.CreateParameterType(rpcCallParameter.Name, rpcCallParameter.ParameterType);
                    parameters[index] = smdParameter;
                    index++;
                }
                method.Parameters = parameters;
                smdService.AddMethod(methodName, method);
            }

            SmdData = smdService.ToUtf8JsonAsync().Result;
        }

        private void RegisterAvailableCalls(MethodInfo[] methods)
        {
            foreach (var method in methods)
            {
                //Get if the method support JsonRpcMethodAttribute
                var methodAttributes = method.GetCustomAttributes(typeof(RpcMethodAttribute), true);
                if (methodAttributes.Length == 0)
                {
                    //If no JsonRpcMethodAttribute, do not add it into the method dictionary.
                    continue;
                }

                if (methodAttributes.Length > 1)
                {
                    // Can not define more than 2 attributes
                    throw new InvalidOperationException($"Method {method.Name} defined more than one rpc method attributes.");
                }

                if (!method.IsPublic)
                {
                    //Rpc method should be public
                    throw new InvalidOperationException($"Method {method.Name} should be public for rpc method.");
                }

                var methodAttribute = (RpcMethodAttribute)methodAttributes[0];
                var methodName = string.IsNullOrWhiteSpace(methodAttribute.Name)
                    ? method.Name
                    : methodAttribute.Name;

                _rpcCalls.Add(methodName, CreateJsonRpcCall(methodName, method));
            }
        }

        private JsonRpcCall CreateJsonRpcCall(string methodName, MethodInfo method)
        {
            var voidMethod = method.ReturnType == typeof(void);
            //Get all parameters
            var parameters = method.GetParameters();

            //Generate CallingParameters
            var paramInfos = new List<JsonRpcCallParameter>();
            foreach (var parameterInfo in parameters)
            {
                paramInfos.Add(new JsonRpcCallParameter(parameterInfo.Name, parameterInfo.ParameterType));
            }

            var dynamicMethod = voidMethod ? 
                new DynamicMethod("", null, new[] { typeof(object), typeof(object[]) }, GetType().Module) : 
                new DynamicMethod("", typeof(object), new[] { typeof(object), typeof(object[]) }, GetType().Module);

            //Generate the delegate by Emit.
            var il = dynamicMethod.GetILGenerator();
            //Put the first arg in stack which is this object..
            il.Emit(OpCodes.Ldarg_0);
            //Cast the object to the real type.
            il.Emit(OpCodes.Castclass, GetType());

            //Put all args which is object[] in stack.
            for (var i = 0; i < paramInfos.Count; i++)
            {
                var parameterInfo = paramInfos[i];
                //Put the arg 1 which is object[] in stack.
                il.Emit(OpCodes.Ldarg_S, 1);
                //Put an integer value in stack which is the index in object[]
                il.Emit(OpCodes.Ldc_I4_S, i);
                //Get the reference of index which is the integer value from the object[].
                il.Emit(OpCodes.Ldelem_Ref);
                //Cast or unbox the reference.
                il.Emit(parameterInfo.ParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameterInfo.ParameterType);
            }
            //Call the method.
            il.Emit(OpCodes.Call, method);
            //Box the result if result is value type.
            if (method.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Box, method.ReturnType);
            }
            //Exit the method.
            il.Emit(OpCodes.Ret);

            //Create the delegate by dynamic method.
            if (voidMethod)
            {
                var voidInvoker = (VoidInvoke)dynamicMethod.CreateDelegate(typeof(VoidInvoke));
                return new JsonRpcCall(this, methodName, paramInfos.ToArray(), new JsonRpcVoidInvokeMethod(voidInvoker));
            }
            var invoker = (Invoke)dynamicMethod.CreateDelegate(typeof(Invoke));
            return new JsonRpcCall(this, methodName, paramInfos.ToArray(), new JsonRpcInvokeMethod(invoker, method.ReturnType));
        }


        /// <summary>
        /// Get the rpc call from the service by name.
        /// </summary>
        /// <param name="name">The name of the rpc call, if is null, will use the parameters to find the call.</param>
        /// <returns>The rpc call, null if not exist.</returns>
        internal JsonRpcCall GetRpcCall(string name)
        {
            return !_rpcCalls.ContainsKey(name) ? null : _rpcCalls[name];
        }

        /// <summary>
        /// Get all rpc calls provided by this service.
        /// </summary>
        /// <returns>All rpc calls</returns>
        internal JsonRpcCall[] GetRpcCalls()
        {
            return _rpcCalls.Values.ToArray();
        }
    }
}
