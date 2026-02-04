using Mars.LogCommon;
using MarsAutomationPerformanceAgentSvc.Mars.Svc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MarsAutomationPerformanceAgentSvc
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public long dwServiceType;
        public ServiceState dwCurrentState;
        public long dwControlsAccepted;
        public long dwWin32ExitCode;
        public long dwServiceSpecificExitCode;
        public long dwCheckPoint;
        public long dwWaitHint;
    };

    

    public partial class MarsAutomationSvcMain : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        private static MLogger Logger = MLogger.GetLogger(typeof(MarsAutomationSvcMain));

        public MarsAutomationSvcMain()
        {
            Logger.logBegin("MarsAutomationSvcMain");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Logger.Info("OnStart",string.Format("args:[{0}]",args));
            this.objEventLog.WriteEntry("In onStart");

            #region notify service management // Update the service state to Start Pending
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            #endregion

            //bool isStart = MonitorThrdMgr.StartDeamonThread();

        }   

        protected override void OnStop()
        {
            Logger.Info("OnStop", "services is stoped");
            this.objEventLog.WriteEntry("In onStop");
            base.OnStop();
        }
        protected override void OnContinue()
        {
            Logger.Info("OnContinue","In onContinue");
            base.OnContinue();
        }
    }
}
