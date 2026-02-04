using System;
using System.Diagnostics;
using System.IO;
using MARSMessageAgent.Packets;

namespace MARSMessageAgent
{
    /// <summary>
    /// 检查 MARS Engine 安装目录并启动。MARS Engine 为 ClickOnce 安装，通常与本 COM 同目录。
    /// </summary>
    public static class MarsEngineLauncher
    {
        private const string MarsEngineExeName = "MARSEngine.exe";
        private const string MarsEngineAppName = "MARSEngine";

        /// <summary>
        /// 获取 MARS Engine 可能的安装目录（与当前程序同目录）。
        /// </summary>
        public static string GetMarsEngineInstallDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(baseDir);
        }

        /// <summary>
        /// 在安装目录下查找 MARS Engine 可执行文件（MARSEngine.exe 或 ClickOnce 应用目录）。
        /// </summary>
        public static string FindMarsEnginePath()
        {
            var baseDir = GetMarsEngineInstallDirectory();

            var directExe = Path.Combine(baseDir, MarsEngineExeName);
            if (File.Exists(directExe))
                return directExe;

            var appDir = Path.Combine(baseDir, MarsEngineAppName);
            if (Directory.Exists(appDir))
            {
                var exeInApp = Path.Combine(appDir, MarsEngineExeName);
                if (File.Exists(exeInApp))
                    return exeInApp;
            }

            return null;
        }

        /// <summary>
        /// 尝试启动 MARS Engine，返回 (成功与否, 消息)。
        /// </summary>
        public static (bool Success, string Message) TryStart()
        {
            var exePath = FindMarsEnginePath();
            if (string.IsNullOrEmpty(exePath))
            {
                var installDir = GetMarsEngineInstallDirectory();
                return (false, $"MARS Engine not found in install directory: {installDir}");
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                return (true, "MARS Engine started successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to start MARS Engine: {ex.Message}");
            }
        }
    }
}
