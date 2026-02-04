using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess
{
    public sealed class MARSTestProcess
    {
        private const int MFC_STANDARD_APP_TYPE = 64;

        public static int CurrentTestProcessId = 0;

        public static bool IsApplicationUsingMFCStandard = false;

        public static void SetIsApplicationUsingMFCStandard(int applicationTypId)
        {
            IsApplicationUsingMFCStandard = applicationTypId == MFC_STANDARD_APP_TYPE;
        }

        public static void InitTestProcess(string strProcessIdName, ref string strError, ref string processDebugInfo)
        {
            if (!IsApplicationUsingMFCStandard) {
                strError = "The application under test is not a standard MFC application. Or not set yet";
                return;
            }
            var ps = Process.GetProcessesByName(strProcessIdName);
            if (ps.Length == 0)
            {
                strError = $"Cannot find process named {strProcessIdName}";
                return;
            }
            /// 只获得当前用户的进程
            /// 
            List<Process> currentUserProcesses = new List<Process>();
            var currentUser = Environment.UserName;
            processDebugInfo = "";
            var curp = Process.GetCurrentProcess();
            foreach (var p in ps)
            {
                try
                {
                    string tmpPInfo = $"{p.ProcessName}|{p.Id}|{p.StartInfo.UserName}|{p.SessionId}|curSessionid:{curp.SessionId}";
                    if (string.IsNullOrEmpty(processDebugInfo))
                    {
                        processDebugInfo = tmpPInfo;
                    }
                    else
                    {
                        processDebugInfo += "\r\n" + tmpPInfo;
                    }
                    if (p.SessionId == curp.SessionId)
                    {
                        currentUserProcesses.Add(p);
                    }
                }
                catch
                {
                    // 可能会因为权限问题无法获取到用户名
                }
            }
            if (currentUserProcesses.Count > 1)
            {
                strError = $"Find more than one process named {strProcessIdName}";
                return;
            }
            if (currentUserProcesses.Count == 0)
            {
                strError = $"Cannot find process named {strProcessIdName} for current user {currentUser}";
                return;
            }
            CurrentTestProcessId = currentUserProcesses[0].Id;
        }
    }
}
