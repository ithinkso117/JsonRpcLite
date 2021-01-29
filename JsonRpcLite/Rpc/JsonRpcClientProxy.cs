using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace JsonRpcLite.Rpc
{
    public abstract class JsonRpcClientProxy
    {
        private readonly string _serviceName;
        private readonly JsonRpcClient _client;

        protected JsonRpcClientProxy()
        {

        }

        protected JsonRpcClientProxy(string serviceName, JsonRpcClient client)
        {
            _serviceName = serviceName;
            _client = client;
        }

        internal static T CreateProxy<T>(string serviceName, JsonRpcClient client)
        {
            var interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException($"{nameof(T)} is not an interface");
            }

            var clientTypeType = typeof(JsonRpcClient);
            var baseType = typeof(JsonRpcClientProxy);

            var interfaceName = interfaceType.Name;
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("JsonRpcLite.Client"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("JsonRpcLite.Client");
            var typeBuilder = moduleBuilder.DefineType($"JsonRpcClientProxy_{interfaceName}", TypeAttributes.Public, baseType,new []{interfaceType});

            //Make the constructor
            var baseConstructor = baseType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), clientTypeType }, null);
            if (baseConstructor == null)
            {
                throw new NullReferenceException($"The constructor for {baseType.Name} does not exits.");
            }

            var constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new[] { typeof(string), clientTypeType }
                );
            var constructorIll = constructorBuilder.GetILGenerator();
            constructorIll.Emit(OpCodes.Ldarg_0);
            constructorIll.Emit(OpCodes.Ldarg_1);
            constructorIll.Emit(OpCodes.Ldarg_2);
            constructorIll.Emit(OpCodes.Call, baseConstructor);
            constructorIll.Emit(OpCodes.Ret);

            //Implement the interface.
            CreateMethods(typeBuilder, interfaceType);
            var type = typeBuilder.CreateType();
            if (type == null)
            {
                throw new InvalidOperationException($"Can not create proxy for {interfaceType.Name}.");
            }
            var proxy = Activator.CreateInstance(type, serviceName, client);
            return (T)proxy;
        }


        private static Type[] ToTypes(IEnumerable<ParameterInfo> parameterInfos)
        {
            return parameterInfos.Select(x => x.ParameterType).ToArray();
        }

        private static void CreateMethods(TypeBuilder typeBuilder, Type type)
        {
            Dictionary<MethodInfo, MethodBuilder> methodBuilders = new Dictionary<MethodInfo, MethodBuilder>();
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                MethodBuilder mdb = CreateMethod(typeBuilder, method);
                methodBuilders.Add(method, mdb);
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var pb = typeBuilder.DefineProperty(property.Name, property.Attributes, property.PropertyType, ToTypes(property.GetIndexParameters()));
                var getMethod = property.GetGetMethod();
                if (getMethod != null && methodBuilders.ContainsKey(getMethod))
                {
                    pb.SetGetMethod(methodBuilders[getMethod]);
                }
                MethodInfo setMethod = property.GetSetMethod();
                if (setMethod != null && methodBuilders.ContainsKey(setMethod))
                {
                    pb.SetSetMethod(methodBuilders[setMethod]);
                }
            }
        }


        private static MethodBuilder CreateMethod(TypeBuilder typeBuilder, MethodInfo method)
        {
            var baseType = typeof(JsonRpcClientProxy);
            var typeofObjectArray = typeof(object[]);
            var typeofObject = typeof(object);
            var voidType = typeof(void);
            var methodName = method.Name;

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

            Type[] paramTypes = ToTypes(method.GetParameters());

            var name = method.Name;
            var methodAttr = MethodAttributes.Public |
                         MethodAttributes.Virtual |
                         MethodAttributes.HideBySig;
            var b = typeBuilder.DefineMethod(name, methodAttr, method.CallingConvention, method.ReturnType, paramTypes);
            if (method.IsGenericMethod)
            {
                Type[] genericArguments = method.GetGenericArguments();
                string[] typeParamNames = new string[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    typeParamNames[i] = genericArguments[i].Name;
                }
                b.DefineGenericParameters(typeParamNames);
            }
            ILGenerator gen = b.GetILGenerator();
            LocalBuilder parameters = gen.DeclareLocal(typeofObjectArray);
            gen.Emit(OpCodes.Ldstr, methodName); //methodName
            gen.Emit(OpCodes.Stloc_0);

            gen.Emit(OpCodes.Ldarg_0); //this
            gen.Emit(OpCodes.Ldloc_0); //method name
            gen.Emit(OpCodes.Ldc_I4, paramTypes.Length); //int = paramTypes.Length
            gen.Emit(OpCodes.Newarr, typeofObject); // new Object[]
            if (paramTypes.Length > 0)
            {
                gen.Emit(OpCodes.Stloc, parameters);
                for (var i = 0; i < paramTypes.Length; ++i)
                {
                    gen.Emit(OpCodes.Ldloc, parameters);
                    gen.Emit(OpCodes.Ldc_I4, i);
                    gen.Emit(OpCodes.Ldarg, i + 1);
                    if (paramTypes[i].IsValueType)
                    {
                        gen.Emit(OpCodes.Box, paramTypes[i]);
                    }
                    gen.Emit(OpCodes.Stelem_Ref);
                }
                gen.Emit(OpCodes.Ldloc, parameters);
            }

            if (method.ReturnType != voidType)
            {
                if (method.ReturnType == typeof(Task))
                {
                    gen.Emit(OpCodes.Callvirt, voidInvokeAsync);
                }
                else if(typeof(Task).IsAssignableFrom(method.ReturnType) && method.ReturnType.IsGenericType)
                {
                    var argumentType = method.ReturnType.GetGenericArguments().Single();
                    var genericInvokeAsync = invokeAsync.MakeGenericMethod(argumentType);
                    gen.Emit(OpCodes.Callvirt, genericInvokeAsync);
                }
                else
                {
                    var genericInvoke = invoke.MakeGenericMethod(method.ReturnType);
                    gen.Emit(OpCodes.Callvirt, genericInvoke);
                }
                
            }
            else
            {
                gen.Emit(OpCodes.Callvirt, voidInvoke);
            }
            gen.Emit(OpCodes.Ret);
            return b;
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
        protected async Task<T> InvokeAsync<T>(string methodName, params object[] args)
        {
            return await _client.InvokeAsync<T>(_serviceName, methodName, args);
        }


        /// <summary>
        /// Invoke remote method without result using sync way.
        /// </summary>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        protected void VoidInvoke(string methodName, params object[] args)
        {
            VoidInvokeAsync(methodName, args).Wait();
        }

        /// <summary>
        /// Invoke remote method without result.
        /// </summary>
        /// <param name="methodName">The method name to call.</param>
        /// <param name="args">The parameters of the method.</param>
        /// <returns>Void</returns>
        protected async Task VoidInvokeAsync(string methodName, params object[] args)
        {
            await _client.VoidInvokeAsync(_serviceName, methodName, args);
        }
    }
}
