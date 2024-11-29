using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PipeCall;

internal static class ProxyGenerator
{
    private static readonly ModuleBuilder moduleBuilder;

    static ProxyGenerator()
    {
        // Console.WriteLine("[ProxyGen] Initializing ModuleBuilder");
        var assemblyName = new AssemblyName("DynamicProxies");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
    }

    public static Type CreateProxyType(Type baseType, object target)
    {
        // Console.WriteLine($"[ProxyGen] Creating proxy type for {baseType.FullName}");
        // Console.WriteLine($"[ProxyGen] Target type: {target.GetType().FullName}");

        var typeName = $"{baseType.Name}Proxy_{Guid.NewGuid():N}";
        // Console.WriteLine($"[ProxyGen] New type name: {typeName}");

        var typeBuilder = moduleBuilder.DefineType(typeName, 
            TypeAttributes.Public | TypeAttributes.Class, 
            baseType);

        // Define field to hold the target
        var targetField = typeBuilder.DefineField("_target", target.GetType(), FieldAttributes.Private);

        // Define constructor
        var ctor = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            new[] { target.GetType() });

        var baseCtor = baseType.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, Type.EmptyTypes, null);

        // Console.WriteLine($"[ProxyGen] Base constructor found: {baseCtor != null}");

        var ctorIL = ctor.GetILGenerator();
        ctorIL.Emit(OpCodes.Ldarg_0);
        ctorIL.Emit(OpCodes.Call, baseCtor);
        ctorIL.Emit(OpCodes.Ldarg_0);
        ctorIL.Emit(OpCodes.Ldarg_1);
        ctorIL.Emit(OpCodes.Stfld, targetField);
        ctorIL.Emit(OpCodes.Ret);

        // Get all abstract methods that need to be implemented
        var methods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsAbstract)
            .ToArray();
        // Console.WriteLine($"[ProxyGen] Found {methods.Length} abstract methods to implement");

        foreach (var method in methods)
        {
            // Console.WriteLine($"[ProxyGen] Implementing method: {method.Name}");
            var parameters = method.GetParameters();
            // Console.WriteLine($"[ProxyGen] Method has {parameters.Length} parameters");

            var methodBuilder = typeBuilder.DefineMethod(
                method.Name,
                MethodAttributes.Public | MethodAttributes.Virtual,
                method.ReturnType,
                parameters.Select(p => p.ParameterType).ToArray());

            var methodIL = methodBuilder.GetILGenerator();

            // Declare locals
            var argsLocal = methodIL.DeclareLocal(typeof(object[]));
            // Console.WriteLine($"[ProxyGen] Declared args array local variable");

            // Create array for method arguments
            methodIL.Emit(OpCodes.Ldc_I4, parameters.Length);
            methodIL.Emit(OpCodes.Newarr, typeof(object));
            methodIL.Emit(OpCodes.Stloc, argsLocal);
            // Console.WriteLine($"[ProxyGen] Created args array of length {parameters.Length}");

            // Store each parameter in the array
            for (int i = 0; i < parameters.Length; i++)
            {
                // Console.WriteLine($"[ProxyGen] Processing parameter {i}: {parameters[i].ParameterType.Name}");
                methodIL.Emit(OpCodes.Ldloc, argsLocal);
                methodIL.Emit(OpCodes.Ldc_I4, i);
                methodIL.Emit(OpCodes.Ldarg, i + 1);

                if (parameters[i].ParameterType.IsValueType)
                {
                    // Console.WriteLine($"[ProxyGen] Boxing value type {parameters[i].ParameterType.Name}");
                    methodIL.Emit(OpCodes.Box, parameters[i].ParameterType);
                }

                methodIL.Emit(OpCodes.Stelem_Ref);
            }

            // Load target field and prepare for InvokeMethod call
            // Console.WriteLine("[ProxyGen] Loading target field and preparing InvokeMethod call");
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, targetField);
            methodIL.Emit(OpCodes.Ldstr, method.Name);
            methodIL.Emit(OpCodes.Ldloc, argsLocal);
            methodIL.Emit(OpCodes.Ldtoken, method.ReturnType);
            methodIL.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));

            var invokeMethod = target.GetType().GetMethod("InvokeMethod");
            // Console.WriteLine($"[ProxyGen] Found InvokeMethod: {invokeMethod != null}");
            if (invokeMethod == null)
            {
                throw new Exception("InvokeMethod not found on target type");
            }

            methodIL.Emit(OpCodes.Callvirt, invokeMethod);
            // Console.WriteLine("[ProxyGen] Called InvokeMethod");

            if (method.ReturnType == typeof(void))
            {
                // Console.WriteLine("[ProxyGen] Void return type, popping result");
                methodIL.Emit(OpCodes.Pop);
            }
            else if (method.ReturnType.IsValueType)
            {
                // Console.WriteLine($"[ProxyGen] Unboxing return value to {method.ReturnType.Name}");
                methodIL.Emit(OpCodes.Unbox_Any, method.ReturnType);
            }
            else
            {
                // Console.WriteLine($"[ProxyGen] Casting return value to {method.ReturnType.Name}");
                methodIL.Emit(OpCodes.Castclass, method.ReturnType);
            }

            methodIL.Emit(OpCodes.Ret);
            // Console.WriteLine("[ProxyGen] Method implementation complete");

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        // Console.WriteLine("[ProxyGen] Creating type");
        var type = typeBuilder.CreateTypeInfo();
        // Console.WriteLine($"[ProxyGen] Type created: {type?.FullName ?? "NULL"}");
        return type;
    }
}