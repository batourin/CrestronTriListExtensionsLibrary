using System;

#if SSHARP
using Crestron.SimplSharp.Reflection;
#else
using System.Reflection;
#endif

namespace Daniels.TriList
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
#if SSHARP
    public class JoinAttribute: CAttribute
#else
    public class JoinAttribute : Attribute
#endif
    {
        public virtual string Name { get; set; }
        public virtual ushort Join { get; set; }
        public virtual eJoinType JoinType { get; set; }

        public virtual eJoinDirection JoinDirection { get; set; }

        public JoinAttribute()
        {
            JoinDirection = eJoinDirection.None;
        }

        public JoinAttribute(string name, ushort join, eJoinType joinType) : this()
        {
            Name = name;
            Join = join;
            JoinType = joinType;
        }

        public override string ToString()
        {
            return String.Format("Name={0}, Join={1}, JoinType={2}{3}", Name, Join, JoinType.ToString(), (JoinDirection != eJoinDirection.None) ? String.Format(" JoinDirection={0}", JoinDirection) : String.Empty);
        }
    }
}
