using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Data.Common;

#if SSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using System.Reflection.Emit;
#endif


namespace Daniels.TriList
{
    public static class TriListComponentFactory
    {
        public static T CreateTriListComponent<T>() where T: TriListComponent
        {

#if SSHARP
            CType t = typeof(T).GetCType();
#else
            Type t = typeof(T);
#endif

            AssemblyName assemblyName = new AssemblyName("CrestronTriListExtensionsComponents");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            // The module name is usually the same as the assembly name.
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(t.Name + "Proxy", TypeAttributes.Public, t);

            MethodInfo digitalJoinSetter = t.GetMethod("SetJoinValue", new Type[] { typeof(ushort), typeof(bool) });
            MethodInfo analogJoinSetter = t.GetMethod("SetJoinValue", new Type[] { typeof(ushort), typeof(ushort) });
            MethodInfo serialJoinSetter = t.GetMethod("SetSerialJoinValue", new Type[] { typeof(ushort), typeof(string) });

            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: digitalJoinSetter: {0}", digitalJoinSetter.Name);
            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: analogJoinSetter: {0}", analogJoinSetter.Name);
            Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: serialJoinSetter: {0}", serialJoinSetter.Name);

            // Loop through all, event methods usualy protected, i.e. non-public
            foreach (MemberInfo memberInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
#if SSHARP
                JoinAttribute joinAttribute = (JoinAttribute)CAttribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#else
                JoinAttribute joinAttribute = (JoinAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#endif

#if SSHARP
#else
                // Create new Virtual property with setters.
                if (memberInfo is PropertyInfo propertyInfo)
                {
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: Property: {0}", propertyInfo.Name);
                    Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: Attribute: {0}", joinAttribute.Name);

                    // Create override property matching original
                    PropertyBuilder joinBaseProperty = typeBuilder.DefineProperty(propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, Type.EmptyTypes);
                    // Define the "get" accessor method for new overriden property
                    MethodBuilder joinPropertyGet = typeBuilder.DefineMethod("get_" + propertyInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyInfo.PropertyType, Type.EmptyTypes);
                    ILGenerator joinPropertyGetILGenerator = joinPropertyGet.GetILGenerator();
                    //The proxy object
                    joinPropertyGetILGenerator.Emit(OpCodes.Ldarg_0);  // "this" in the stack
                    //The database
                    //joinPropertyGetILGenerator.Emit(OpCodes.Ldfld, database);
                    //The proxy object
                    joinPropertyGetILGenerator.Emit(OpCodes.Ldarg_0);
                    //The ObjectId to look for
                    //joinPropertyGetILGenerator.Emit(OpCodes.Ldfld, f);
                    //pILGet.Emit(OpCodes.Callvirt, typeof(MongoDatabase).GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ObjectId) }, null).MakeGenericMethod(propertyInfo.PropertyType));
                    joinPropertyGetILGenerator.Emit(OpCodes.Ret);

                    // Define the "set" accessor method for new overriden property
                    MethodBuilder joinPropertySet = typeBuilder.DefineMethod("set_" + propertyInfo.Name, MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyInfo.PropertyType });
                    ILGenerator joinPropertySetILGenerator = joinPropertySet.GetILGenerator();
                    joinPropertySetILGenerator.Emit(OpCodes.Ldarg_0); // "this" in the stack
                    joinPropertySetILGenerator.Emit(OpCodes.Ldarg_1);
                    switch(joinAttribute.JoinType)
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
                    joinPropertySetILGenerator.Emit(OpCodes.Ldarg_0);
                    //pILSet.Emit(OpCodes.Ldfld, database);
                    //pILSet.Emit(OpCodes.Call, typeof(ProxyBuilder).GetMethod("SetValueHelper", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(object), typeof(MongoDatabase) }, null));
                    //pILSet.Emit(OpCodes.Stfld, f);
                    joinPropertySetILGenerator.Emit(OpCodes.Ret);

                    // Last, we must map the two methods created above to our PropertyBuilder to
                    // their corresponding behaviors, "get" and "set" respectively.
                    joinBaseProperty.SetGetMethod(joinPropertyGet);
                    joinBaseProperty.SetSetMethod(joinPropertySet);

                }
#endif
                t = typeBuilder.CreateType();

                // Save the assembly so it can be examined with Ildasm.exe,
                // or referenced by a test program.
                // assemblyBuilder.Save(assemblyName.Name + ".dll");
            }
            return (T)Activator.CreateInstance(t);
        }
    }
}
