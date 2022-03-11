using System;
using System.Collections.Generic;
using System.Linq;

#if SSHARP
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharp.Reflection.Emit;
#else
using System.Reflection;

using System.Reflection.Emit;
using System.Runtime.InteropServices;
#endif


namespace Daniels.TriList
{
    public static class TriListComponentFactory
    {

        private static readonly ModuleBuilder _moduleBuilder;
        private static readonly Dictionary<Type, Type> _typeCache = new Dictionary<Type, Type>();

        static TriListComponentFactory()
        {
            AssemblyName assemblyName = new AssemblyName("CrestronTriListExtensionsComponents");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            // The module name is usually the same as the assembly name.
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
        }
        public static T CreateTriListComponent<T>(params object[] args) where T: TriListComponent
        {
            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: enter");

#if SSHARP
            CType originalType = typeof(T).GetCType();
#else
            Type originalType = typeof(T);
            Type proxyType;
#endif

            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: pre-lock");
            lock (_typeCache)
            {
                Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: typeCache locked");
                if (!_typeCache.TryGetValue(originalType, out proxyType))
                {
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: cache empty");
                    TypeBuilder typeBuilder = _moduleBuilder.DefineType(originalType.Name + "Proxy", TypeAttributes.Public, originalType);
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: typeBuild created: {0}", typeBuilder.Name);

                    MethodInfo[] methods = typeBuilder.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (MethodInfo mi in methods)
                    {
                        //Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: {0}: {1}:{2}:{3}", mi.Name, mi.IsPublic, mi.IsPrivate, mi.IsFamily);
                        foreach (ParameterInfo pi in mi.GetParameters())
                        {
                            //Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: \t{0}: {1}", pi.Name, pi.ParameterType.ToString());
                        }
                    }
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: getting setters");

                    MethodInfo digitalJoinSetter = methods.Where(m => m.Name == "SetDigitalJoinValue").FirstOrDefault();
                    MethodInfo analogJoinSetter = methods.Where(m => m.Name == "SetAnalogJoinValue").FirstOrDefault();
                    MethodInfo serialJoinSetter = methods.Where(m => m.Name == "SetSerialJoinValue").FirstOrDefault();

                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: digitalJoinSetter: {0}", digitalJoinSetter.Name);
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: analogJoinSetter: {0}", analogJoinSetter.Name);
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: serialJoinSetter: {0}", serialJoinSetter.Name);

                    Dictionary<ushort, FieldBuilder> fieldsToSubScribe = new Dictionary<ushort, FieldBuilder>();

                    // Loop through all, event methods usualy protected, i.e. non-public
                    foreach (MemberInfo memberInfo in originalType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
#if SSHARP
                        JoinAttribute joinAttribute = (JoinAttribute)CAttribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#else
                        JoinAttribute joinAttribute = (JoinAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#endif

#if SSHARP
#else
                        // Create new Virtual property with setters.
                        if (joinAttribute != null && memberInfo is PropertyInfo propertyInfo)
                        {
                            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: Property: {0}", propertyInfo.Name);
                            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: Attribute: {0}", joinAttribute.Name);

                            // Create override property matching original
                            PropertyBuilder joinBaseProperty = typeBuilder.DefineProperty(propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, Type.EmptyTypes);
                            if(propertyInfo.CanRead)
                            {
                                // Create private field for new property getter as storage data from joins
                                FieldBuilder joinField = typeBuilder.DefineField($"_{propertyInfo.Name}ProxyValue", propertyInfo.PropertyType, FieldAttributes.Private);

                                // Define the "get" accessor method for new overriden property
                                MethodBuilder joinPropertyGet = typeBuilder.DefineMethod("get_" + propertyInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyInfo.PropertyType, Type.EmptyTypes);
                                ILGenerator joinPropertyGetILGenerator = joinPropertyGet.GetILGenerator();
                                // "this" in to the stack
                                joinPropertyGetILGenerator.Emit(OpCodes.Ldarg_0);
                                // return backed field
                                joinPropertyGetILGenerator.Emit(OpCodes.Ldfld, joinField);
                                joinPropertyGetILGenerator.Emit(OpCodes.Ret);
                                // Assign getter to the property
                                joinBaseProperty.SetGetMethod(joinPropertyGet);

                                fieldsToSubScribe[joinAttribute.Join] = joinField;
                            }
                            if (propertyInfo.CanWrite)
                            {
                                // Define the "set" accessor method for new overriden property
                                MethodBuilder joinPropertySet = typeBuilder.DefineMethod("set_" + propertyInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyInfo.PropertyType });
                                ILGenerator joinPropertySetILGenerator = joinPropertySet.GetILGenerator();
                                // "this" in to the stack
                                joinPropertySetILGenerator.Emit(OpCodes.Ldarg_0);
                                // join number from the attribute as int
                                joinPropertySetILGenerator.Emit(OpCodes.Ldc_I4, joinAttribute.Join);
                                // convert loaded in stack int value back to ushort
                                joinPropertySetILGenerator.Emit(OpCodes.Conv_U2);
                                // value of the setter
                                joinPropertySetILGenerator.Emit(OpCodes.Ldarg_1);
                                switch (joinAttribute.JoinType)
                                {
                                    case eJoinType.Digital:
                                        joinPropertySetILGenerator.Emit(OpCodes.Callvirt, digitalJoinSetter);
                                        break;
                                    case eJoinType.Analog:
                                        joinPropertySetILGenerator.Emit(OpCodes.Callvirt, analogJoinSetter);
                                        break;
                                    case eJoinType.Serial:
                                        joinPropertySetILGenerator.Emit(OpCodes.Callvirt, serialJoinSetter);
                                        break;
                                }
                                joinPropertySetILGenerator.Emit(OpCodes.Ret);
                                // Assign setter to the property
                                joinBaseProperty.SetSetMethod(joinPropertySet);
                            }
                        }
#endif
                    }

                    // Adding original constructors
                    ConstructorInfo[] constructors = originalType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                    foreach (ConstructorInfo constructorInfo in originalType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                    {
                        ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
                        Type[] constructorParameterTypes = parameterInfos.Select(pi => pi.ParameterType).ToList().ToArray();
                        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, constructorParameterTypes);
                        ILGenerator constructorBuilderILGenerator = constructorBuilder.GetILGenerator();
                        constructorBuilderILGenerator.Emit(OpCodes.Ldarg_0);
                        for (int i = 1; i <= constructorParameterTypes.Length; i++)
                            constructorBuilderILGenerator.Emit(OpCodes.Ldarg, i);
                        constructorBuilderILGenerator.Emit(OpCodes.Call, constructorInfo);

                        // Subscribe property fields to base events
                        MethodInfo digitalJoinSubscriber = methods.Where(m => m.Name == "SubscribeDigitalJoin").FirstOrDefault();
                        MethodInfo analogJoinSubscriber = methods.Where(m => m.Name == "SubscribeAnalogJoin").FirstOrDefault();
                        MethodInfo serialJoinSubscriber = methods.Where(m => m.Name == "SubscribeSerialJoin").FirstOrDefault();

                        foreach (KeyValuePair<ushort, FieldBuilder> kv in fieldsToSubScribe)
                        {
                            // "this" in to the stack
                            constructorBuilderILGenerator.Emit(OpCodes.Ldarg_0);
                            // join number from the attribute as int
                            constructorBuilderILGenerator.Emit(OpCodes.Ldc_I4, kv.Key);
                            // convert loaded in stack int value back to ushort
                            constructorBuilderILGenerator.Emit(OpCodes.Conv_U2);

                            constructorBuilderILGenerator.Emit(OpCodes.Ld);

                            if (kv.Value.FieldType == typeof(bool))
                                constructorBuilderILGenerator.Emit(OpCodes.Callvirt, digitalJoinSubscriber);
                            else if(kv.Value.FieldType == typeof(ushort))
                                constructorBuilderILGenerator.Emit(OpCodes.Callvirt, analogJoinSubscriber);
                            else if (kv.Value.FieldType == typeof(string))
                                constructorBuilderILGenerator.Emit(OpCodes.Callvirt, serialJoinSubscriber);
                        }

                        // return
                        constructorBuilderILGenerator.Emit(OpCodes.Ret);
                    }

                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: creating type", typeBuilder.Name);
                    proxyType = typeBuilder.CreateType();
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: type created", typeBuilder.Name);

                    foreach(ConstructorInfo ci in proxyType.GetConstructors())
                    {
                        Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: ctor: {0}", ci.Name);
                        foreach (ParameterInfo pi in ci.GetParameters())
                            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: \t: {0}:{1}", pi.Name, pi.ParameterType.ToString());
                    }

                    // Save the assembly so it can be examined with Ildasm.exe,
                    // or referenced by a test program.
                    // assemblyBuilder.Save(assemblyName.Name + ".dll");
                    _typeCache[originalType] = proxyType;
                }
            }

            foreach(object arg in args)
                Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: ctor arg:{0}:{1}", arg.ToString(), arg.GetType().Name);
            return (T)Activator.CreateInstance(proxyType, args);
        }
    }
}
