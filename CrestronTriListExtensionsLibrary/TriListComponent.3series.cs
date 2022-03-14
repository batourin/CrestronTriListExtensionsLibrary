using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharp.Reflection;

//using System.Runtime.CompilerServices;


using Daniels.Common;

namespace Daniels.TriList
{
    public abstract class TriListComponent3: TriListComponent
    {
        /// <summary>
        /// Constructor to initialize offsets and trilists
        /// </summary>
        /// <param name="digitalOffset">Digital offset in TriList</param>
        /// <param name="analogOffset">Analog offset in TriList</param>
        /// <param name="serialOffset">Analog offset in TriList</param>
        /// <param name="triLists">TriLists to operate on</param>
        protected TriListComponent3(uint digitalOffset, uint analogOffset, uint serialOffset, IEnumerable<BasicTriList> triLists)
            :base(digitalOffset, analogOffset, serialOffset, triLists)
        {
            Bind();
        }

        /// <summary>
        /// Constructor for component where offsets are equal
        /// </summary>
        /// <param name="offset">Digital, Analog and Serial offsets be equal</param>
        /// <param name="triLists">TriLists to operate on</param>
        protected TriListComponent3(uint offset, IEnumerable<BasicTriList> triLists)
            :this(offset, offset, offset, triLists)
        { }

        /// <summary>
        /// Bind trilist feedbacks to fields and event methods
        /// </summary>
        private void Bind()
        {
            // Get all methods in this class, and put them
            // in an array of System.Reflection.MemberInfo objects.
            CType t = this.GetType().GetCType();
            // Loop through all, event methods usualy protected, i.e. non-public
            foreach(MemberInfo memberInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                JoinAttribute joinAttribute = (JoinAttribute)CAttribute.GetCustomAttribute(memberInfo, typeof(JoinAttribute));
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
        }

        protected void SetJoinValue(bool value)
        {
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if (joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
        }

        protected void SetJoinValue(ushort value)
        {
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if (joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
        }

        protected void SetJoinValue(string value)
        {
            //SSharpReflectionExtensions has bug in parsing StackFrame - Current is actually calling method returned...
            JoinAttribute[] joinAttributes = MethodBaseEx.GetCurrentMethod().GetCustomAttributes(typeof(JoinAttribute).GetCType(), false) as JoinAttribute[];
            if (joinAttributes != null && joinAttributes.Length > 0)
                SetJoinValue(joinAttributes[0], value);
        }
    }
}
