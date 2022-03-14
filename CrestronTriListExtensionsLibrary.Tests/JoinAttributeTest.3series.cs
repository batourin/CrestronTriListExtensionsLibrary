using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

using Daniels.Common;

using Crestron.SimplSharp.Reflection;


namespace Daniels.TriList.Tests
{
    public class JoinAttributeTest: TriListComponent3
    {

        public JoinAttributeTest(uint digitalOffset, uint analogOffset, uint serialOffset, IEnumerable<BasicTriList> remotes)
            : base(digitalOffset, analogOffset, serialOffset, remotes)
        {

        }

        [Join(Name = "String Property Feedback", Join = 1, JoinType = eJoinType.Serial, JoinDirection = eJoinDirection.From)]
        private string _propertyTest;
        public string PropertyTest
        {
            get { return _propertyTest; }
            [Join(Name = "String Property Setter", Join = 1, JoinType = eJoinType.Serial)]
            set 
            {
                CrestronConsole.PrintLine("Setting Join#{0} to {1}", 1, value);
                SetJoinValue(value);
            }
        }

        public ushort SetOnlyProperty
        {
            [Join(Name = "SetOnlyProperty Test", Join = 1, JoinType = eJoinType.Analog)]
            set
            {
                SetJoinValue(value);
            }
        }

        public event EventHandler<ReadOnlyEventArgs<bool>> TestFeedbackCommanded;

        [Join(Name = "Digital Feedback Test", Join = 1, JoinType = eJoinType.Digital, JoinDirection = eJoinDirection.From)]
        protected virtual void OnTestFeedbackCommanded(ReadOnlyEventArgs<bool> e)
        {
            EventHandler<ReadOnlyEventArgs<bool>> handler = TestFeedbackCommanded;
            if (handler != null)
                handler(this, e);
        }

        //[Join(Name = "Set Test Value", Join = 1, JoinType = eJoinType.Analog, JoinDirection = eJoinDirection.To)]
        public void SetTestValue()
        {

        }

    }
}