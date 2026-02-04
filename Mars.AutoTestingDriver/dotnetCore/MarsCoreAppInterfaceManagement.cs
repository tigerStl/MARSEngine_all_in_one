#if !_unit_test
using Route2NSEx.src.Marquis.systemUtil;
#endif
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.AutoTestingDriver.WebHelpers;

namespace Mars.AutoTestingDriver.dotnetCore
{
    public class MarsCoreAppInterfaceManagement
    {
        public const string cnst_host_agent_core = "MarsEngine.Core.dll";
        public const string cnst_host_agent_starter = "MarsEngineCore.HostLauncher.exe";
        private static List<int> WPF_CORE_TARGET_app_pids = null;
#if !_unit_test
        private static MLogger logger = MLogger.GetLogger(typeof(MarsCoreAppInterfaceManagement));
#endif
        internal static bool HostToTargetApplication(string strCurMarsAccount, string strProcessName, ref string strError, ref string strAdv, ref string strStack, 
            string strTitleOfWindow="", bool isToCheck=false, int waitSeconds =10)
        {
#if !_unit_test
            logger.logBegin("HostToTargetApplication", $"currentMarsAccount|{strCurMarsAccount}|processName|{strProcessName}|");
#endif
            string strProcessNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(strProcessName);
            Process curP = Process.GetCurrentProcess();
            Process[] arrPx = Process.GetProcessesByName(strProcessNameWithoutExtension);
            long n = DateTime.Now.Ticks, p = n;
            while ((p - n) < (waitSeconds * TimeSpan.TicksPerSecond))
            {
#if !_unit_test
                logger.Info("HostToTargetApplication", $"find|process|{strProcessName}|{arrPx.Length}");
#endif
                arrPx = arrPx.Where(p => (p.SessionId == curP.SessionId)).ToArray();
                foreach (var px in arrPx)
                {
#if !_unit_test
                    logger.Info("HostToTargetApplication", $"{px.Id}|{px.MainWindowHandle}|{px.MainWindowTitle}");
#endif
                }
                arrPx = arrPx.Where(p=>!p.MainWindowHandle.Equals(IntPtr.Zero))
                    .ToArray();
#if !_unit_test
                logger.Info("HostToTargetApplication", $"after filter by zero handle|{arrPx.Length}");
#endif
                if (arrPx.Length > 0) break;

                System.Threading.Thread.Sleep(1000);
                arrPx = Process.GetProcessesByName(strProcessNameWithoutExtension);
                p = DateTime.Now.Ticks;
            }
#if !_unit_test
            var prntId = WPF_CORE_processHelper.GetRootProcessWithGUI(arrPx,strTitleOfWindow, isToCheck);

            //if (WPF_CORE_TARGET_app_pids == null || WPF_CORE_TARGET_app_pids.Count == 0) {
            //    WPF_CORE_TARGET_app_pids = arrPx.Select(p=>p.Id).ToList();
            //}            
            if (prntId > 0)
            {
                var px = arrPx.FirstOrDefault(p => p.Id == prntId);
                logger.Info("HostToTargetApplication", $"supposed the process is|{px.MainWindowTitle}|{px.MainWindowHandle}");
            }
#endif
            var targetP = arrPx.FirstOrDefault();

            if (targetP == null) {
                strAdv = $"Please ensure that |{strProcessName}| has been started";
                strError = $"no such application is running|{strProcessName}.{strAdv}";
#if !_unit_test
                logger.Error("HostToTargetApplication", strError);
#endif
                return false;                
            }

            /// 判断模块是否插入

            //            if (IsMarsModualAlreadyHosted2(targetP))
            //            {
            //#if !_unit_test
            //                logger.Info("HostToTargetApplication","MARS DLL HAS BEEN TARGED");
            //#endif
            //                return true;
            //            }

            /// 调用
            /// .\MarsEngineCore.HostLauncher.exe --assembly MarsEngine.Core 
            /// --attachConsoleToParent --className MarsEngine.Infrastructure.MarsCoreInnerManager --targetHwnd 199408 
            /// --methodName StartMarsEngineSvc 
            /// --targetPID 24616
            /// 
            string strCoreHostPath = typeof(MarsCoreAppInterfaceManagement).Assembly.Location;
            strCoreHostPath = System.IO.Path.GetDirectoryName(strCoreHostPath);
            strCoreHostPath = System.IO.Path.Combine(strCoreHostPath, "MarsCore");
            string starterExeFile = System.IO.Path.Combine(strCoreHostPath, cnst_host_agent_starter);
            if (!System.IO.File.Exists(starterExeFile))
            {
                strAdv = $"Please ensure that MARS .net core Engine exists at {starterExeFile}";
                strError = $"no Mars .net core Engine starter exists at |{starterExeFile}. {strAdv}";
                return false;
            }

            /// start .net core engine
            /// 
            if (!StartCoreEngine(targetP, starterExeFile, strCoreHostPath, ref strError, ref strAdv))
            {
                return false;
            }
            var pid = targetP.Id;
            targetP.Close();

            /// 调用configFile获得web的端口
            /// 
            bool isOk = false;
            int iPort = WPF_CORE_processHelper.GetRemoteDebuggerPortFromConfigFile(ref isOk, ref strError);
            if (!isOk)
            {
                strAdv = $"Please ensure that MARS .net core Engine exists and MarsEngineCoreConfig.json exists";
                strError = $"no Mars .net core Engine config file exists. {strAdv}";
                return false;
            }

            MARSKeywordWebHelpers.currentMarsSeleniumWarp.remote_debugger_port = iPort;


            //            var parrNew = Process.GetProcessesByName(strProcessNameWithoutExtension);
            //            arrPx = arrPx.Where(p => p.SessionId == curP.SessionId).ToArray();
            //            var pNew = arrPx.FirstOrDefault();
            //            //var pNew = Process.GetProcessById(pid);
            //            if (!IsMarsModualAlreadyHosted2(pNew))
            //            {
            //                strAdv = $"Please ensure that |{cnst_host_agent_core}| be installed correctly.";
            //#if !_unit_test
            //                logger.Error("HostToTargetApplication", strError = $"after starter, there still no |{cnst_host_agent_core}| is on service.|{strAdv}");
            //#endif
            //                return false;
            //            }
            //            else
            //            {
            //#if !_unit_test
            //                logger.Info("HostToTargetApplication", "MARS DLL HAS BEEN TARGED, AFTER HOST");
            //#endif
            //            }

            return true;
        }

        private static List<string> GetProcessModules(int processId)
        {
            List<string> modules = new List<string>();

            // 使用 ClrMD 连接到目标进程
            using (DataTarget dataTarget = DataTarget.AttachToProcess(processId, suspend: false))
            {
                // 获取 CLR 运行时信息
                ClrInfo clrInfo = dataTarget.ClrVersions[0]; // 假设只有一个 CLR 运行时
                ClrRuntime runtime = clrInfo.CreateRuntime();

                // 获取模块列表
                foreach (ClrModule module in runtime.EnumerateModules())
                {
                    modules.Add(module.Name);
                }
            }

            return modules;
        }

        //internal static bool IsMarsModualAlreadyHosted2(Process process)
        //{
        //    // 使用 ClrMD 获取进程的模块列表
        //    var lstModule = GetProcessModules(process.Id);

        //    logger.Debug("IsMarsModualAlreadyHosted2", string.Join(",",lstModule));
        //    var targetModules = new string[] { cnst_host_agent_core, cnst_host_agent_starter };
        //    bool isOk = lstModule.Any(p => targetModules.Any(x=>string.Compare(x, p, true)==0));
        //    return isOk;
        //}

        internal static bool IsMarsModualAlreadyHosted(Process process)
        {
            if (process == null) return false;
            var targetModules = new string[] { cnst_host_agent_core, cnst_host_agent_starter };
            // 获取进程的所有模块
            ProcessModuleCollection modules = process.Modules;

            // 检查是否存在指定的模块
            foreach (string targetModule in targetModules)
            {
                //bool moduleExists = false;
                foreach (ProcessModule module in modules)
                {
#if !_unit_test
                    logger.Info("IsMarsModualAlreadyHosted", module.ModuleName);
#endif
                    if (module.ModuleName == null) continue;
                    if (module.ModuleName.Equals(targetModule, StringComparison.OrdinalIgnoreCase))
                    {                        
                        return true;
                    }
                }                
            }
            return false;
        }

        internal static bool StartCoreEngine(Process targetp, string strPath, string strWorkDir, ref string strError, ref string strAdv)
        {
            try
            {
                IntPtr mainHdl = targetp.MainWindowHandle;
                long pid = targetp.Id;
                string strPara = $"--assembly MarsEngine.Core --attachConsoleToParent --className MarsEngine.Infrastructure.MarsCoreInnerManager --targetHwnd {mainHdl} --methodName StartMarsEngineSvc --targetPID {pid}";
#if !_unit_test
                logger.Info("StartCoreEngine", $"{strWorkDir}\r\n\t{strPath}  {strPara}");
#endif
                // 创建 ProcessStartInfo 对象
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = strPath, // 进程路径
                    WorkingDirectory = strWorkDir, // 工作目录
                    Arguments = strPara, // 命令行参数
                    UseShellExecute = false, // 不使用操作系统 shell 启动进程
                    RedirectStandardOutput = true, // 重定向标准输出
                    RedirectStandardError = true, // 重定向标准错误
                    CreateNoWindow = false // 创建新窗口, for test
                };

                // 启动进程
                using (Process process = new Process { StartInfo = startInfo })
                {
#if !_unit_test
                    logger.Info("StartCoreEngine", $"start {strPath} ...");
#endif
                    process.Start();

                    // 读取标准输出和标准错误
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // 等待进程结束
                    process.WaitForExit();
#if !_unit_test
                    logger.Info("StartCoreEngine", $"{output}\r\n{error}");
                    logger.Info("StartCoreEngine", $"process has quit with code|{process.ExitCode}");
#endif
                }
                return true;
            }
            catch (Exception ex)
            {
                strAdv = $"Please ensure application|{strPath}| can be started";
                strError = $"{ex.Message}|{strAdv}";
                //Console.WriteLine($"发生错误: {ex.Message}");
                return false;
            }
        }
    }
}
