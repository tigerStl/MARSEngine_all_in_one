using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;

namespace MarsSpyTool.Utility.License
{
    /// <summary>
    /// 反调试和反篡改保护类
    /// </summary>
    public static class AntiDebugProtection
    {
        private static readonly Logger logger = LogManager.GetLogger("MarsSpyLog");
        private static Timer _checkTimer;
        private static bool _isProtectionActive = false;

        #region Windows API

        [DllImport("kernel32.dll")]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        #endregion

        /// <summary>
        /// 启动反调试保护（推荐在 Application_Startup 中调用）
        /// </summary>
        public static void StartProtection()
        {
            if (_isProtectionActive)
            {
                logger.Warn("StartProtection\tProtection already active");
                return;
            }

            logger.Info("StartProtection\tStarting anti-debug protection");

            try
            {
                // 立即检查一次
                if (DetectDebugger())
                {
                    logger.Error("StartProtection\tDebugger detected on startup");
                    HandleDebuggerDetected();
                    return;
                }

                // 启动定期检查（每秒）
                _checkTimer = new Timer(PeriodicCheck, null, 1000, 1000);
                _isProtectionActive = true;

                logger.Info("StartProtection\tProtection started successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"StartProtection\tException: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止反调试保护
        /// </summary>
        public static void StopProtection()
        {
            logger.Info("StopProtection\tStopping protection");
            _checkTimer?.Dispose();
            _checkTimer = null;
            _isProtectionActive = false;
        }

        /// <summary>
        /// 定期检查回调
        /// </summary>
        private static void PeriodicCheck(object state)
        {
            try
            {
                if (DetectDebugger())
                {
                    logger.Error("PeriodicCheck\tDebugger detected during runtime");
                    StopProtection();
                    HandleDebuggerDetected();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"PeriodicCheck\tException: {ex.Message}");
            }
        }

        /// <summary>
        /// 检测调试器（多种方法）
        /// </summary>
        private static bool DetectDebugger()
        {
            // 方法1: Debugger.IsAttached
            if (Debugger.IsAttached)
            {
                logger.Warn("DetectDebugger\tMethod1: Debugger.IsAttached = true");
                return true;
            }

            // 方法2: IsDebuggerPresent
            if (IsDebuggerPresent())
            {
                logger.Warn("DetectDebugger\tMethod2: IsDebuggerPresent = true");
                return true;
            }

            // 方法3: CheckRemoteDebuggerPresent
            bool isDebuggerPresent = false;
            try
            {
                CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
                if (isDebuggerPresent)
                {
                    logger.Warn("DetectDebugger\tMethod3: CheckRemoteDebuggerPresent = true");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DetectDebugger\tCheckRemoteDebuggerPresent failed");
            }

            // 方法4: 检查父进程（如果父进程是调试器）
            try
            {
                var parentProcess = GetParentProcess();
                if (parentProcess != null)
                {
                    string parentName = parentProcess.ProcessName.ToLower();
                    // 常见调试器进程名
                    string[] debuggerNames = { "devenv", "windbg", "x64dbg", "x32dbg", "ollydbg", "ida", "idaq", "idaq64" };
                    
                    foreach (var debuggerName in debuggerNames)
                    {
                        if (parentName.Contains(debuggerName))
                        {
                            logger.Warn($"DetectDebugger\tMethod4: Parent process is debugger: {parentName}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DetectDebugger\tParent process check failed");
            }

            // 方法5: 时间检测（调试时代码执行会变慢）
            var sw = Stopwatch.StartNew();
            var dummy = 0;
            for (int i = 0; i < 100; i++)
            {
                dummy += i;
            }
            sw.Stop();

            // 正常执行应该非常快（< 1ms），如果超过说明可能在单步调试
            if (sw.ElapsedMilliseconds > 100)
            {
                logger.Warn($"DetectDebugger\tMethod5: Execution too slow: {sw.ElapsedMilliseconds}ms");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取父进程
        /// </summary>
        private static Process GetParentProcess()
        {
            try
            {
                var pbi = new PROCESS_BASIC_INFORMATION();
                int returnLength;
                int status = NtQueryInformationProcess(
                    Process.GetCurrentProcess().Handle,
                    0, // ProcessBasicInformation
                    ref pbi,
                    Marshal.SizeOf(pbi),
                    out returnLength);

                if (status == 0)
                {
                    int parentPid = pbi.InheritedFromUniqueProcessId.ToInt32();
                    return Process.GetProcessById(parentPid);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 处理检测到调试器的情况
        /// </summary>
        private static void HandleDebuggerDetected()
        {
            logger.Error("HandleDebuggerDetected\tTaking action...");

            // 策略1: 直接退出（最简单）
            Environment.Exit(1);

            // 策略2: 挂起（让破解者以为程序死了）
            // Thread.Sleep(Timeout.Infinite);

            // 策略3: 显示误导性消息
            // System.Windows.MessageBox.Show(
            //     "License 验证失败。\n\n错误代码: 0x80070005",
            //     "错误",
            //     System.Windows.MessageBoxButton.OK,
            //     System.Windows.MessageBoxImage.Error);
            // Environment.Exit(0);

            // 策略4: 破坏数据（极端情况）
            // 不推荐，可能导致用户数据丢失
        }

        /// <summary>
        /// 检查程序集完整性
        /// </summary>
        public static bool VerifyIntegrity()
        {
            logger.Info("VerifyIntegrity\tBegin");

            try
            {
                // 方法1: 检查强名称签名
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                byte[] publicKey = assembly.GetName().GetPublicKey();

                if (publicKey == null || publicKey.Length == 0)
                {
                    logger.Error("VerifyIntegrity\tAssembly not signed with strong name");
                    return false;
                }

                // 方法2: 检查文件哈希（需要在编译时硬编码预期值）
                // 这里简化处理，实际应该对比真实的哈希值
                // string exePath = assembly.Location;
                // string actualHash = ComputeFileHash(exePath);
                // const string EXPECTED_HASH = "YOUR_HASH_HERE";
                // if (actualHash != EXPECTED_HASH) return false;

                // 方法3: 检查关键资源是否存在
                var resourceNames = assembly.GetManifestResourceNames();
                if (resourceNames.Length == 0)
                {
                    logger.Warn("VerifyIntegrity\tNo embedded resources found");
                }

                logger.Info("VerifyIntegrity\tIntegrity check passed");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"VerifyIntegrity\tException: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算文件哈希
        /// </summary>
        private static string ComputeFileHash(string filePath)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        /// <summary>
        /// 防止内存转储
        /// </summary>
        public static void PreventMemoryDump()
        {
            // 注意: 这只是示例，实际效果有限
            // 真正的防护需要使用商业保护工具

            try
            {
                // 使 GC 更频繁地清理敏感数据
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                logger.Info("PreventMemoryDump\tMemory cleaned");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"PreventMemoryDump\tException: {ex.Message}");
            }
        }
    }
}

