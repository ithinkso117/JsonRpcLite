using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLite.Services;

namespace JsonRpcLite.Rpc
{
    public abstract class JsonRpcClientProxy
    {

        private static readonly Dictionary<Type, Type> TypeCache = new();

        private readonly int _timeout;
        private readonly string _serviceName;
        private readonly JsonRpcClient _client;


        private JsonRpcClientProxy()
        {

        }

        protected JsonRpcClientProxy(string serviceName, JsonRpcClient client, int timeout)
        {
            _timeout = timeout;
            _serviceName = serviceName;
            _client = client;
        }

        /// <summary>
        /// Create a proxy for given interface.
        /// </summary>
        /// <typeparam name="T">The interface type.</typeparam>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="client">The JsonRpcClient which will be used by the proxy.</param>
        /// <param name="timeout">The timeout of the proxy</param>
        /// <returns>The proxy which implement the given interface.</returns>
        internal static T CreateProxy<T>(string serviceName, JsonRpcClient client, int timeout = Timeout.Infinite)
        {
            var interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException($"{nameof(T)} is not an interface");
            }

            if (!TypeCache.TryGetValue(interfaceType, out var proxyType))
            {
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcLite.Client"),AssemblyBuilderAccess.Run);
                var moduleBuilder = assemblyBuilder.DefineDynamicModule("JsonRpcLite.Client.Proxy");

                var interfaceName = interfaceType.Name;
                var uniqueName = Guid.NewGuid().ToString("N");
                var typeBuilder = moduleBuilder.DefineType(
                    $"JsonRpcClientProxy_{interfaceName}_{uniqueName}",
                    TypeAttributes.Public,
                    typeof(JsonRpcClientProxy),
                    new[] {interfaceType});

                //Create the constructor
                CreateProxyConstructor(typeBuilder);

                //Implement the interface.
                ImplementInterface<T>(typeBuilder);
                //Create the type.
                proxyType = typeBuilder.CreateType();
                if (proxyType == null)
                {
                    throw new InvalidOperationException($"Can not create proxy for {interfaceType.Name}.");
                }
                TypeCache.Add(interfaceType,proxyType);
            }

            var proxy = Activator.CreateInstance(proxyType, serviceName, client, timeout);
            return (T)proxy;
        }


        /// <summary>
        /// Create the proxy constructor for given type.
        /// </summary>
        /// <param name="typeBuilder">The type builder which will add the constructor</param>
        private static void CreateProxyConstructor(TypeBuilder typeBuilder)
        {
            var baseConstructor = typeof(JsonRpcClientProxy).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(JsonRpcClient), typeof(int) },
                null
            );
            if (baseConstructor == null)
            {
                throw new NullReferenceException($"The constructor for {nameof(JsonRpcClientProxy)} does not exits.");
            }
            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new[] { typeof(string), typeof(JsonRpcClient)}
            );
            var constructorIll = constructorBuilder.GetILGenerator();
            constructorIll.Emit(OpCodes.Ldarg_0);
            constructorIll.Emit(OpCodes.Ldarg_1);
            constructorIll.Emit(OpCodes.Ldarg_2);
            constructorIll.Emit(OpCodes.Ldarg_3);
            constructorIll.Emit(OpCodes.Call, baseConstructor);
            constructorIll.Emit(OpCodes.Ret);
        }


        /// <summary>
        /// Implement interface for given type.
        /// </summary>
        /// <typeparam name="T">The interface type</typeparam>
        /// <param name="typeBuilder">The type builder to implement the interface</param>
        private static void ImplementInterface<T>(TypeBuilder typeBuilder)
        {
            var interfaceType = typeof(T);
            var methodBuilders = new Dictionary<MethodInfo, MethodBuilder>();
            foreach (var method in interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                methodBuilders.Add(method, CreateMethod(typeBuilder, method));
            }

            //If the interface contains properties, it means it has set method and get method.
            foreach (var property in interfaceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var indexParameterTypes = property.GetIndexParameters().Select(x => x.ParameterType).ToArray();
                var propertyBuilder = typeBuilder.DefineProperty(property.Name, property.Attributes, property.PropertyType, indexParameterTypes);
                var getMethod = property.GetGetMethod();
                if (getMethod != null && methodBuilders.ContainsKey(getMethod))
                {
                    propertyBuilder.SetGetMethod(methodBuilders[getMethod]);
                }
                var setMethod = property.GetSetMethod();
                if (setMethod != null && methodBuilders.ContainsKey(setMethod))
                {
                    propertyBuilder.SetSetMethod(methodBuilders[setMethod]);
                }
            }
        }


        /// <summary>
        /// Create a method for the given type.
        /// </summary>
        /// <param name="typeBuilder">The type builder which will add the method.</param>
        /// <param name="method">The method to create.</param>
        /// <returns>The method builder.</returns>
        private static MethodBuilder CreateMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var baseType = typeof(JsonRpcClientProxy);
            var methodName = method.Name;
            //Get if the method support JsonRpcMethodAttribute
            var methodAttributes = method.GetCustomAttributes(typeof(RpcMethodAttribute), true);
            if (methodAttributes.Length > 1)
            {
                // Can not define more than 2 attributes
                throw new InvalidOperationException($"Method {methodName} defined more than one rpc method attributes.");
            }
            if (methodAttributes.Length == 1)
            {
                var methodAttribute = (RpcMethodAttribute)methodAttributes[0];
                if (!string.IsNullOrEmpty(methodAttribute.Name))
                {
                    methodName = methodAttribute.Name;
                }
            }
            var invoke = baseType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
            if (invoke == null)
            {
                throw new InvalidOperationException("Invoke method does not exist.");
            }

            var voidInvoke = baseType.GetMethod("VoidInvoke", BindingFlags.Instance | BindingFlags.NonPublic);
            if (voidInvoke == null)
            {
                throw new InvalidOperationException("VoidInvoke method does not exist.");
            }

            var invokeAsync = baseType.GetMethod("InvokeAsync" ,BindingFlags.Instance | BindingFlags.NonPublic);
            if (invokeAsync == null)
            {
                throw new InvalidOperationException("InvokeAsync method does not exist.");
            }

            var voidInvokeAsync = baseType.GetMethod("VoidInvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            if (voidInvokeAsync == null)
            {
                throw new InvalidOperationException("VoidInvokeAsync method does not exist.");
            }

            var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
            var name = method.Name;
            var methodAttr = MethodAttributes.Public |
                         MethodAttributes.Virtual |
                         MethodAttributes.HideBySig;
            var methodBuilder = typeBuilder.DefineMethod(name, methodAttr, method.CallingConvention, method.ReturnType, parameterTypes);
            if (method.IsGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();
                var typeParamNames = new string[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    typeParamNames[i] = genericArguments[i].Name;
                }
                methodBuilder.DefineGenericParameters(typeParamNames);
            }
            var methodIl = methodBuilder.GetILGenerator();
            var parameters = methodIl.DeclareLocal(typeof(object[]));
            methodIl.Emit(OpCodes.Ldstr, methodName); //methodName
            methodIl.Emit(OpCodes.Stloc_0);

            methodIl.Emit(OpCodes.Ldarg_0); //this
            methodIl.Emit(OpCodes.Ldloc_0); //method name
            methodIl.Emit(OpCodes.Ldc_I4, parameterTypes.Length); //int = paramTypes.Length
            methodIl.Emit(OpCodes.Newarr, typeof(object)); // new Object[]

            //fill parameters into the object[].
            if (parameterTypes.Length > 0)
            {
                methodIl.Emit(OpCodes.Stloc, parameters);
                for (var i = 0; i < parameterTypes.Length; ++i)
                {
                    methodIl.Emit(OpCodes.Ldloc, parameters);
                    methodIl.Emit(OpCodes.Ldc_I4, i);
                    //The index 0 is this object, so should be i+1.
                    methodIl.Emit(OpCodes.Ldarg, i + 1);
                    if (parameterTypes[i].IsValueType)
                    {
                        methodIl.Emit(OpCodes.Box, parameterTypes[i]);
                    }
                    methodIl.Emit(OpCodes.Stelem_Ref);
                }
                methodIl.Emit(OpCodes.Ldloc, parameters);
            }

            if (method.ReturnType != typeof(void))
            {
                if (method.ReturnType == typeof(Task))
                {
                    methodIl.Emit(OpCodes.Callvirt, voidInvokeAsync);
                }
                else if(typeof(Task).IsAssignableFrom(method.ReturnType) && method.ReturnType.IsGenericType)
                {
                    var argumentType = method.ReturnType.GetGenericArguments().Single();
                    var genericInvokeAsync = invokeAsync.MakeGenericMethod(argumentType);
                    methodIl.Emit(OpCodes.Callvirt, genericInvokeAsync);
                }
                else
                {
                    var genericInvoke = invoke.MakeGenericMethod(method.ReturnType);
                    methodIl.Emit(OpCodes.Callvirt, genericInvoke);
                }
                
            }
            else
            {
                methodIl.Emit(OpCodes.Callvirt, voidInvoke);
            }
            methodIl.Emit(OpCodes.Ret);
            return methodBuilder;
        }


        /// <summary>
        /// Invoke remote method using sync way.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>The result value.</returns>
        protected T Invoke<T>(string methodName, params object[] args)
        {
            return InvokeAsync<T>(methodName, args).Result;
        }

        /// <summary>
        /// Invoke remote method and get the result.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>The result value.</returns>
        protected async Task<T> InvokeAsync<T>(string methodName, object[] args)
        {
            if (_timeout == Timeout.Infinite)
            {
                return await _client.InvokeAsync<T>(_serviceName, methodName, args, CancellationToken.None);
            }
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(_timeout));
            return await _client.InvokeAsync<T>(_serviceName, methodName, args, cancellationTokenSource.Token);
        }


        /// <summary>
        /// Invoke remote method without result using sync way.
        /// </summary>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        protected void VoidInvoke(string methodName, params object[] args)
        {
            if (_timeout == Timeout.Infinite)
            {
                VoidInvokeAsync(methodName, args).Wait();
            }
            else
            {
                VoidInvokeAsync(methodName, args).Wait(_timeout);
            }
        }

        /// <summary>
        /// Invoke remote method without result.
        /// </summary>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>Void</returns>
        protected async Task VoidInvokeAsync(string methodName, params object[] args)
        {
            if (_timeout == Timeout.Infinite)
            {
                await _client.VoidInvokeAsync(_serviceName, methodName, CancellationToken.None, args);
            }
            else
            {
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(_timeout));
                await _client.VoidInvokeAsync(_serviceName, methodName, cancellationTokenSource.Token, args);
            }
        }
    }
}
