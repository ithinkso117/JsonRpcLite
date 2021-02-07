using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace JsonRpcLite.Services
{
    internal interface IJsonRpcCallService
    {
        /// <summary>
        /// Gets the service mapping description data.
        /// </summary>
        byte[] SmdData { get; }

        /// <summary>
        /// Gets the rpc call by name.
        /// </summary>
        /// <param name="name">The name of the call.</param>
        /// <returns>The found rpc call, null if the rpc call does not exist.</returns>
        JsonRpcCall this[string name] { get; }
    }

    internal class JsonRpcCallManager
    {
        public class JsonRpcCallService : IJsonRpcCallService
        {
            private readonly Dictionary<string, JsonRpcCall> _calls = new();

            /// <summary>
            /// Gets the name of of the method service.
            /// </summary>
            public string Name { get; }


            /// <summary>
            /// Gets the service mapping description data.
            /// </summary>
            public byte[] SmdData { get; private set; }

            /// <summary>
            /// Gets the rpc call by name.
            /// </summary>
            /// <param name="name">The name of the call.</param>
            /// <returns>The found rpc call, null if the rpc call does not exist.</returns>
            public JsonRpcCall this[string name]
            {
                get
                {
                    if (_calls.TryGetValue(name, out var rpcCall))
                    {
                        return rpcCall;
                    }

                    return null;
                }
            }


            public JsonRpcCallService(string name)
            {
                Name = name;
            }


            /// <summary>
            /// Add call into the service.
            /// </summary>
            /// <param name="call">The call to add.</param>
            public void AddRpcCall(JsonRpcCall call)
            {
                if (_calls.ContainsKey(call.Name))
                {
                    throw new InvalidOperationException($"Method {call.Name} exists.");
                }
                _calls.Add(call.Name, call);
            }


            /// <summary>
            /// Generate the smd data.
            /// </summary>
            public void BuildSmd()
            {
                var smdService = new SmdService(Name);
                foreach (var rpcCall in _calls.Values)
                {
                    var methodName = rpcCall.Name;
                    var parameters = new ISmdParameter[rpcCall.Parameters.Count];
                    ISmdType returns = null;
                    if (rpcCall.ReturnType != null && rpcCall.ReturnType != typeof(void) && rpcCall.ReturnType != typeof(Task))
                    {
                        returns = smdService.CreateSmdType(rpcCall.ReturnType);
                    }
                    var index = 0;
                    foreach (var rpcCallParameter in rpcCall.Parameters)
                    {
                        var parameter = smdService.CreateSmdParameter(rpcCallParameter.Name, smdService.CreateSmdType(rpcCallParameter.ParameterType));
                        parameters[index] = parameter;
                        index++;
                    }

                    var smdMethod = smdService.CreateSmdMethod(methodName, parameters, returns);
                    smdService.AddMethod(smdMethod);
                }

                SmdData = smdService.ToUtf8JsonAsync().Result;
            }
        }


        private static readonly Dictionary<string, JsonRpcCallService> CallServices = new();


        private static JsonRpcCall CreateJsonRpcCall(string methodName, object serviceInstance, MethodInfo method)
        {
            var voidMethod = method.ReturnType == typeof(void);
            //Get all parameters
            var parameters = method.GetParameters();

            //Generate CallingParameters
            var paramInfos = new List<JsonRpcCallParameter>();
            foreach (var parameterInfo in parameters)
            {
                var parameterTypeChecker = new JsonRpcTypeChecker();
                if (!parameterTypeChecker.IsParameterTypeAllowed(parameterInfo.ParameterType))
                {
                    throw new InvalidOperationException($"Parameter [{parameterInfo.Name}] - [{parameterInfo.ParameterType}] for method [{method.Name}] is not supported. Reason: {parameterTypeChecker.Error}");
                }
                paramInfos.Add(new JsonRpcCallParameter(parameterInfo.Name, parameterInfo.ParameterType));
            }

            var returnTypeChecker = new JsonRpcTypeChecker();
            if (!returnTypeChecker.IsReturnTypeAllowed(method.ReturnType))
            {
                throw new InvalidOperationException($"Return type {method.ReturnType} is not supported. Reason: {returnTypeChecker.Error}");
            }

            var serviceType = serviceInstance.GetType();
            var dynamicMethod = voidMethod ?
                new DynamicMethod("", null, new[] { typeof(object), typeof(object[]) }, serviceType.Module) :
                new DynamicMethod("", typeof(object), new[] { typeof(object), typeof(object[]) }, serviceType.Module);

            //Generate the delegate by Emit.
            var il = dynamicMethod.GetILGenerator();
            //Put the first arg in stack which is this object..
            il.Emit(OpCodes.Ldarg_0);
            //Cast the object to the real type.
            il.Emit(OpCodes.Castclass, serviceType);

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
            if (!voidMethod && method.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Box, method.ReturnType);
            }
            //Exit the method.
            il.Emit(OpCodes.Ret);

            //Create the delegate by dynamic method.
            if (voidMethod)
            {
                var voidInvoker = (VoidInvoke)dynamicMethod.CreateDelegate(typeof(VoidInvoke));
                return new JsonRpcCall(serviceInstance, methodName, paramInfos.ToArray(), new JsonRpcVoidInvokeMethod(voidInvoker));
            }
            var invoker = (Invoke)dynamicMethod.CreateDelegate(typeof(Invoke));
            return new JsonRpcCall(serviceInstance, methodName, paramInfos.ToArray(), new JsonRpcInvokeMethod(invoker, method.ReturnType));
        }

        /// <summary>
        /// Register a rpc service and its methods into the manager.
        /// </summary>
        /// <param name="service">The rpc service to register.</param>
        public static void RegisterService(object service)
        {
            var serviceType = service.GetType();
            if (serviceType.IsGenericType)
            {
                throw new InvalidOperationException("Generic type service is not supported.");
            }
            var serviceName = serviceType.Name;
            var serviceAttributes = serviceType.GetCustomAttributes(typeof(RpcServiceAttribute), false);
            if (serviceAttributes.Length == 0)
            {
                throw new InvalidOperationException("Can not register service without [RpcService] attribute.");
            }
            if (serviceAttributes.Length > 1)
            {
                throw new InvalidOperationException($"Service {serviceName} defined more than one rpc service attributes.");
            }
            if (serviceAttributes.Length > 0)
            {
                var serviceAttribute = (RpcServiceAttribute)serviceAttributes[0];
                if (!string.IsNullOrEmpty(serviceAttribute.Name))
                {
                    serviceName = serviceAttribute.Name;
                }
            }

            if (CallServices.ContainsKey(serviceName))
            {
                throw new InvalidOperationException($"Service {serviceName} already exists.");
            }

            //For object service, only method with RpcMethodAttribute can be registered.
            var callService = new JsonRpcCallService(serviceName);
            //Load all methods.
            var methods = serviceType.GetMethods();
            //Register available methods.
            var availableMethods = methods.Where(x => x.IsVirtual == false && x.IsStatic == false).ToArray();
            foreach (var method in availableMethods)
            {
                if (method.IsGenericMethod)
                {
                    //WE DO NOT SUPPORT GENERIC-METHOD
                    continue;
                }
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

                callService.AddRpcCall(CreateJsonRpcCall(methodName, service, method));
            }
            callService.BuildSmd();
            CallServices.Add(serviceName, callService);
        }


        /// <summary>
        /// Register a service and its methods into the manager.
        /// </summary>
        /// <typeparam name="T">The interface of the service.</typeparam>
        /// <param name="service">The service to register.</param>
        public static void RegisterService<T>(T service)
        {
            //For service with interface, only method on interface can be registered.
            var interfaceType = typeof(T);

            if (interfaceType.IsGenericType)
            {
                throw new InvalidOperationException("Generic type service is not supported.");
            }

            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException($"{interfaceType} is not an interface.");
            }
            var serviceType = service.GetType();

            var serviceName = interfaceType.Name;
            //Remove the start "I" char for the interface name if exists.
            if (serviceName.StartsWith("i", StringComparison.InvariantCultureIgnoreCase) && serviceName.Length > 1)
            {
                serviceName = serviceName.Substring(1);
            }

            var serviceAttributes = interfaceType.GetCustomAttributes(typeof(RpcServiceAttribute), false);
            if (serviceAttributes.Length > 1)
            {
                throw new InvalidOperationException($"Service {serviceName} defined more than one rpc service attributes.");
            }
            if (serviceAttributes.Length > 0)
            {
                var serviceAttribute = (RpcServiceAttribute)serviceAttributes[0];
                if (!string.IsNullOrEmpty(serviceAttribute.Name))
                {
                    serviceName = serviceAttribute.Name;
                }
            }

            if (CallServices.ContainsKey(serviceName))
            {
                throw new InvalidOperationException($"Service {serviceName} already exists.");
            }

            var callService = new JsonRpcCallService(serviceName);
            //Load all methods.
            var interfaceMethods = interfaceType.GetMethods();
            foreach (var interfaceMethod in interfaceMethods)
            {
                if (interfaceMethod.IsGenericMethod)
                {
                    //WE DO NOT SUPPORT GENERIC-METHOD
                    throw new InvalidOperationException("GenericMethod is not supported.");
                }
                var method = serviceType.GetMethod(interfaceMethod.Name);
                //Get if the method support JsonRpcMethodAttribute
                var methodAttributes = interfaceMethod.GetCustomAttributes(typeof(RpcMethodAttribute), true);
                if (methodAttributes.Length > 1)
                {
                    // Can not define more than 2 attributes
                    throw new InvalidOperationException($"Method {interfaceMethod.Name} defined more than one rpc method attributes.");
                }

                var methodName = interfaceMethod.Name;
                if (methodAttributes.Length == 1)
                {
                    var methodAttribute = (RpcMethodAttribute)methodAttributes[0];
                    if (!string.IsNullOrEmpty(methodAttribute.Name))
                    {
                        methodName = methodAttribute.Name;
                    }
                }
                callService.AddRpcCall(CreateJsonRpcCall(methodName, service, method));
            }
            callService.BuildSmd();
            CallServices.Add(serviceName, callService);
        }

        /// <summary>
        /// Get the method service by service name.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The found method service, null if does not exist.</returns>
        public static IJsonRpcCallService GetService(string serviceName)
        {
            if (CallServices.TryGetValue(serviceName, out var service))
            {
                return service;
            }
            return null;
        }
    }
}
