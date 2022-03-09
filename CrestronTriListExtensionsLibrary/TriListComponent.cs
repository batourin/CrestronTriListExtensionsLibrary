using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

#if SSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using System.Reflection.Emit;
#endif

using Daniels.Common;

namespace Daniels.TriList
{
    public abstract class TriListComponent
    {
        /// <summary>
        /// Holds BasicTriList communications
        /// </summary>
        protected readonly List<BasicTriList> _triLists = new List<BasicTriList>();

        /// <summary>
        /// Holds digital offset of the commponent in the tri-list
        /// </summary>
        protected readonly uint _digitalOffset;
        /// <summary>
        /// Holds anolog offset of the commponent in the tri-list
        /// </summary>
        protected readonly uint _analogOffset;
        /// <summary>
        /// Holds serial offset of the commponent in the tri-list
        /// </summary>
        protected readonly uint _serialOffset;

        /// <summary>
        /// Constructor to initialize offsets and trilists
        /// </summary>
        /// <param name="digitalOffset">Digital offset in TriList</param>
        /// <param name="analogOffset">Analog offset in TriList</param>
        /// <param name="serialOffset">Analog offset in TriList</param>
        /// <param name="triLists">TriLists to operate on</param>
        protected TriListComponent(uint digitalOffset, uint analogOffset, uint serialOffset, IEnumerable<BasicTriList> triLists)
        {
            _digitalOffset = digitalOffset;
            _analogOffset = analogOffset;
            _serialOffset = serialOffset;

            _triLists.AddRange(triLists);

            Bind();
        }

        /// <summary>
        /// Constructor for component where offsets are equal
        /// </summary>
        /// <param name="offset">Digital, Analog and Serial offsets be equal</param>
        /// <param name="triLists">TriLists to operate on</param>
        protected TriListComponent(uint offset, IEnumerable<BasicTriList> triLists)
            :this(offset, offset, offset, triLists)
        { }

        /// <summary>
        /// Bind trilist feedbacks to fields and event methods
        /// </summary>
        private void Bind()
        {
            // Get all methods in this class, and put them
            // in an array of System.Reflection.MemberInfo objects.
#if SSHARP
            CType t = this.GetType().GetCType();
#else
            Type t = this.GetType();
#endif
            // Loop through all, event methods usualy protected, i.e. non-public
            foreach(MemberInfo memberInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
#if SSHARP
                JoinAttribute joinAttribute = (JoinAttribute)CAttribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#else
                JoinAttribute joinAttribute = (JoinAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
#endif
                if (joinAttribute != null && joinAttribute.JoinDirection.HasFlag(eJoinDirection.From))
                {
                    //Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: Attribute=\"{0}\": linking Join={1} to {2}", joinAttribute.Name, _digitalOffset + joinAttribute.Join, memberInfo.Name);
                    switch (joinAttribute.JoinType)
                    {
                        case eJoinType.Digital:
                            foreach (var triList in _triLists)
                                triList.BooleanOutput[_digitalOffset + joinAttribute.Join].UserObject = CreateMemberAction<bool>(memberInfo);
                            break;
                        case eJoinType.Analog:
                            foreach (var triList in _triLists)
                                triList.UShortOutput[_analogOffset + joinAttribute.Join].UserObject = CreateMemberAction<ushort>(memberInfo);
                            break;
                        case eJoinType.Serial:
                            foreach (var triList in _triLists)
                                triList.StringOutput[_serialOffset + joinAttribute.Join].UserObject = CreateMemberAction<string>(memberInfo);
                            break;
                    }
                }

            }

            // Subscribe to events from trilist and execute Actions assinged on UserObject property
            foreach (BasicTriList triList in _triLists)
                triList.SigChange += (s, e) =>
                {
                    if (e.Sig.UserObject is Action<bool>)
                        (e.Sig.UserObject as Action<bool>)(e.Sig.BoolValue);
                    else if (e.Sig.UserObject is Action<ushort>)
                        (e.Sig.UserObject as Action<ushort>)(e.Sig.UShortValue);
                    else if (e.Sig.UserObject is Action<string>)
                        (e.Sig.UserObject as Action<string>)(e.Sig.StringValue);
                };
        }

        private Action<T> CreateMemberAction<T>(MemberInfo memberInfo)
        {
            Action<T> action = null;
            if (memberInfo is MethodInfo)
                action = (value) => (memberInfo as MethodInfo).Invoke(this, new object[] { new ReadOnlyEventArgs<T>(value) });
            else if (memberInfo is FieldInfo)
                action = (value) => (memberInfo as FieldInfo).SetValue(this, value);
            return action;
        }

        protected void SetJoinValue(bool value)
        {
#if SSHARP
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if (joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
#else
            //SetJoinValue(MethodBase.GetCurrentMethod().GetCustomAttribute<JoinAttribute>(), value);
#endif
        }

#if SSHARP
        protected void SetJoinValue(ushort value)
        {
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if(joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
        }
#else
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetJoinValue(ushort value, [CallerMemberName] string methodName = null)
        {
            MemberInfo memberInfo = this.GetType().GetMembers().Where(m => m.Name == methodName).FirstOrDefault();
            if(memberInfo != null)
                Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: CallingMethod: {0}", memberInfo.Name);
            SetJoinValue(memberInfo.GetCustomAttribute<JoinAttribute>(), value);
        }
#endif

#if SSHARP
        protected void SetJoinValue(string value)
        {
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if (joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
        }
#else
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void SetJoinValue(string value, [CallerMemberName] string methodName = null)
        {
            MemberInfo memberInfo = this.GetType().GetMembers().Where(m => m.Name == methodName).FirstOrDefault();
            if (memberInfo != null)
                Crestron.SimplSharp.CrestronConsole.PrintLine("CrestronTriListExtentions: CallingMethod: {0}", memberInfo.Name);
            SetJoinValue(memberInfo.GetCustomAttribute<JoinAttribute>(), value);
        }
#endif

        protected void SetJoinValue(JoinAttribute joinAttribute, bool value)
        {
            SetJoinValue(joinAttribute.Join, value);
        }

        protected void SetJoinValue(JoinAttribute joinAttribute, ushort value)
        {
            SetJoinValue(joinAttribute.Join, value);
        }

        protected void SetJoinValue(JoinAttribute joinAttribute, string value)
        {
            SetSerialJoinValue(joinAttribute.Join, value);
        }

        protected void SetJoinValue(ushort joinNumber, bool value)
        {
            foreach (BasicTriList triList in _triLists)
                triList.BooleanInput[_digitalOffset + joinNumber].BoolValue = value;
        }
        protected void SetJoinValue(ushort joinNumber, ushort value)
        {
            foreach(BasicTriList triList in _triLists)
                triList.UShortInput[_analogOffset + joinNumber].UShortValue = value;
        }

        protected void SetSerialJoinValue(ushort joinNumber, string value)
        {
            foreach (BasicTriList triList in _triLists)
                triList.StringInput[_serialOffset + joinNumber].StringValue = value;
        }
    }
}
