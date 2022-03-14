using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.DeviceSupport;

using Daniels.Common;

using System.Reflection;


namespace Daniels.TriList.Tests
{
    public class JoinAttributeTest : TriListComponent
    {

        public JoinAttributeTest(uint digitalOffset, uint analogOffset, uint serialOffset, IEnumerable<BasicTriList> remotes)
            : base(digitalOffset, analogOffset, serialOffset, remotes)
        {

        }

        [Join(Name = "String Property", Join = 3, JoinType = eJoinType.Serial)]
        public virtual string PropertyTest { get; set; }

        [Join(Name = "UShort Readonly Property", Join = 3, JoinType = eJoinType.Serial)]
        public virtual ushort GetOnlyProperty { get; }

        [Join(Name = "Digital SetOnly Property", Join = 3, JoinType = eJoinType.Serial, JoinDirection = eJoinDirection.To)]
        public virtual ushort SetOnlyProperty { get; set; }

        [Join(Name = "Digital SetOnly Function", Join = 1, JoinType = eJoinType.Serial, JoinDirection = eJoinDirection.To)]
        public virtual void SetOnlyFunction (bool value) { }

        public event EventHandler<ReadOnlyEventArgs<bool>> TestFeedbackCommanded;
        [Join(Name = "Digital Feedback Test", Join = 1, JoinType = eJoinType.Digital, JoinDirection = eJoinDirection.From)]
        protected virtual void OnTestFeedbackCommanded(ReadOnlyEventArgs<bool> e)
        {
            EventHandler<ReadOnlyEventArgs<bool>> handler = TestFeedbackCommanded;
            if (handler != null)
                handler(this, e);
        }
    }
}