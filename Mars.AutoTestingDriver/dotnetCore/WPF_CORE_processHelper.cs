
//using Mars.message.windowsWrapper.SystemUtil;

using MarsEnginer.windowsWrapper.SystemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.dotnetCore
{
    public class MarsCoreAgentConfig
    {
        public MarsCoreTestStepGeneratorConfig TestStepGeneratorConfig { get; set; }
        public int WebRemoteDebuggerPort { get; set; }
        /// <summary>
        /// 是否显示本身的命令行窗口
        /// </summary>
        public bool IsShowCommandWidow { get; set; }
        /// <summary>
        /// 是否显示消息中心的命令行窗口
        /// </summary>
        public bool IsShowMessageCenterCommandWindow { get; set; }
        public string LoggerLevel { get; set; }
    }

    public class MarsCoreTestStepGeneratorConfig
    {
        public bool IsHightLight { get; set; }
    }

    public class WPF_CORE_processHelper
    {
        private static MLogger logger = MLogger.GetLogger(typeof(WPF_CORE_processHelper));
        
        public static int GetRemoteDebuggerPortFromConfigFile(ref bool isOk, ref string strError)
        {
            isOk = false;
            strError = "";
            string pth = typeof(WPF_CORE_processHelper).Assembly.Location;
            pth = System.IO.Path.GetDirectoryName(pth);
            string strSwpFile = System.IO.Path.Combine(pth, "MarsCore", "MarsEngineCoreConfig.json");
            if (!System.IO.File.Exists(strSwpFile))
            {
                strError = $"Can't find |{strSwpFile}|";
                return -1;
            }
            try
            {
                var json = System.IO.File.ReadAllText(strSwpFile);
                var portInfo = System.Text.Json.JsonSerializer.Deserialize<MarsCoreAgentConfig>(json);
                if (portInfo == null)
                {
                    strError = $"Can't deserialize remote_debugger_port.json";
                    return -1;
                }
                isOk = true;
                return portInfo.WebRemoteDebuggerPort;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }
        }

        public static int FindRootProcess(Process[] prc)
        {
            List<int> appids = new List<int>();
            foreach (var p in prc)
            {
                var prntid = GetRootProcess(p);
                if (string.IsNullOrEmpty(p.MainWindowTitle)) continue;
                if (prntid.MainWindowHandle.Equals(IntPtr.Zero)) continue;
                
                if (appids.IndexOf(prntid.Id)<0)
                    appids.Add(prntid.Id);
                //Console.WriteLine($"{p.ProcessName}|{p.Id}|{p.MainModule.ModuleName}|{p.MainWindowHandle}---parent|{prntid.Id}");
            }
            return appids.FirstOrDefault();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prc"></param>
        /// <param name="titleCheck"></param>
        /// <param name="isCheckTileEmpty"></param>
        /// <returns></returns>
        public static int GetRootProcessWithGUI(Process[] prc, string titleCheck="", bool isCheckTileEmpty=false)
        {
            List<int> appids = new List<int>();
            foreach (var p in prc)
            {
                if (p.MainWindowHandle.Equals(IntPtr.Zero)) continue;
                string title = p.MainWindowTitle;
                if (string.IsNullOrEmpty(titleCheck))
                {
                    if (isCheckTileEmpty)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!isCheckTileEmpty)
                    {
                        appids.Add(p.Id);
                    }
                    else
                    {
                        if (titleCheck.Equals(title, StringComparison.OrdinalIgnoreCase) || (MarsWindowsAPIsExtend.RegularTest(titleCheck, title)))
                        {
                            appids.Add(p.Id);
                        }
                    }
                }
            }
            if (appids.Count <= 0) {
                //说明没有找到
                return -1;
            }
            logger.Info("GetRootProcessWithGUI", $"target process has|{appids.Count}| left");
            return appids.FirstOrDefault();
        }

        static Process GetRootProcess(Process process)
        {
            int parentId = GetParentProcessId(process.Id);
            while (parentId != 0)
            {
                Process parentProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == parentId);
                if (parentProcess == null)
                    break;

                process = parentProcess;
                parentId = GetParentProcessId(parentProcess.Id);
            }
            return process;
        }
        static int GetParentProcessId(int processId)
        {
            string query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                ManagementObjectCollection result = searcher.Get();
                foreach (ManagementObject mo in result)
                {
                    return Convert.ToInt32(mo["ParentProcessId"]);
                }
            }
            return 0;
        }
    }
}
