using System;
using System.Text;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharpPro.EthernetCommunication;

using Daniels.Common;

namespace Daniels.TriList.Tests
{
    public class ControlSystem : CrestronControlSystem
    {

        ThreeSeriesTcpIpEthernetIntersystemCommunications eisc;

        /// <summary>
        /// ControlSystem Constructor. Starting point for the SIMPL#Pro program.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// 
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// 
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 100;

                eisc = new ThreeSeriesTcpIpEthernetIntersystemCommunications(250, "127.0.0.2", this);
                eDeviceRegistrationUnRegistrationResponse registerResult = eisc.Register();
                if (registerResult != eDeviceRegistrationUnRegistrationResponse.Success)
                    CrestronConsole.PrintLine("Failed to register EISC client: {0}", registerResult.ToString());

                eisc.SigChange += (s, e) =>
                {
                    StringBuilder sb = new StringBuilder("\r\n");
                    sb.AppendFormat("TriList: Event=\"{0}\", Name=\"{1}\", Number=\"{2}\"", e.Event, e.Sig.Name, e.Sig.Number);
                    switch (e.Event)
                    {
                        case eSigEvent.BoolChange:
                            sb.AppendFormat(", Value=\"{0}\"", e.Sig.BoolValue);
                            break;
                        case eSigEvent.UShortChange:
                            sb.AppendFormat(", Value=\"{0}\"", e.Sig.ShortValue);
                            break;
                        case eSigEvent.StringChange:
                            sb.AppendFormat(", Value=\"{0}\"", e.Sig.StringValue);
                            break;
                        case eSigEvent.UShortInputRamping:
                            sb.AppendFormat(", Value=\"{0}\"", e.Sig.RampingInformation.ToString());
                            break;
                    }
                    sb.AppendFormat(", UserObject={0}", (e.Sig.UserObject == null) ? "null" : "action");
                    CrestronConsole.PrintLine(sb.ToString());
                };


                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(ControlSystem_ControllerEthernetEventHandler);

                CrestronConsole.AddNewConsoleCommand(ConsoleCommandTest, "test", "test", ConsoleAccessLevelEnum.AccessOperator);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// 
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
            try
            {

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType"></param>
        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType"></param>
        void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }

        /// <summary>
        /// Test Console functions.
        /// </summary>
        /// <param name="cmd">command name</param>
        private void ConsoleCommandTest(string cmd)
        {
            try
            {
                JoinAttributeTest testComponent = new JoinAttributeTest(10, 20, 30, new BasicTriList[] { eisc });
                CrestronConsole.PrintLine("PropertyTest={0}", testComponent.PropertyTest);
                testComponent.PropertyTest = "Test Name setter property";
                CrestronConsole.PrintLine("PropertyTest={0}", testComponent.PropertyTest);
                testComponent.SetOnlyProperty = 16;
            }
            catch (Exception e)
            {
                CrestronConsole.ConsoleCommandResponse("Error: {0}\r\n{1}", e.Message, e.StackTrace);
            }
        }
    }
}