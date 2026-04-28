using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace MARS.WebAutomation
{
    /// <summary>
    /// Configures NLog for this assembly: writes under <c>{dll directory}\log\marsWebAutomationEngine_yyyyMMdd.log</c>.
    /// If the host process already has an NLog configuration, only appends a file target and a rule for loggers named <c>MARS.WebAutomation*</c>.
    /// </summary>
    internal static class WebAutomationNLog
    {
        private const string LogDirGdcKey = "MarsWebAutomation.LogDir";
        private const string TargetName = "marsWebAutomationEngineFile";

        internal const string LoggerNamePrefix = "MARS.WebAutomation";

        private static readonly object Sync = new object();
        private static bool _configured;

        /// <summary>Resolved log directory (<c>{dll}\log</c>) after <see cref="EnsureConfigured"/>.</summary>
        internal static string EngineLogDirectory { get; private set; }

        internal static void EnsureConfigured()
        {
            if (_configured)
                return;

            lock (Sync)
            {
                if (_configured)
                    return;

                var logDir = GetLogDirectory();
                Directory.CreateDirectory(logDir);
                EngineLogDirectory = logDir;
                GlobalDiagnosticsContext.Set(LogDirGdcKey, logDir);

                var fileTarget = new FileTarget(TargetName)
                {
                    FileName = "${gdc:item=" + LogDirGdcKey + "}/marsWebAutomationEngine+${date:format=yyyyMMdd}.log",
                    Layout =
                        "[${replace:searchFor=WARN:replaceWith=WARNING:inner=${level:uppercase=true}}]\t" +
                        "${longdate}\t" +
                        "${callsite:className=true:methodName=false:includeNamespace=true:cleanNamesOfAnonymousDelegates=true}|" +
                        "${callsite:className=false:methodName=true:includeNamespace=false:cleanNamesOfAnonymousDelegates=true}|" +
                        "${callsite-linenumber}\t" +
                        "${message}|${exception:format=stacktrace}",
                    Encoding = System.Text.Encoding.UTF8,
                    KeepFileOpen = false,
                    ConcurrentWrites = true,
                    AutoFlush = true
                };

                var config = LogManager.Configuration ?? new LoggingConfiguration();
                config.AddTarget(fileTarget);
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget, LoggerNamePrefix + "*");

                LogManager.Configuration = config;
                LogManager.ReconfigExistingLoggers();
                _configured = true;
            }
        }

        private static string GetLogDirectory()
        {
            var dllDir = GetDllDirectory();
            return Path.Combine(dllDir, "log");
        }

        private static string GetDllDirectory()
        {
            try
            {
                var a = typeof(WebAutomationNLog).Assembly;
                var loc = a.Location;
                if (!string.IsNullOrEmpty(loc))
                {
                    var dir = Path.GetDirectoryName(loc);
                    if (!string.IsNullOrEmpty(dir))
                        return dir;
                }

                if (!string.IsNullOrEmpty(a.CodeBase) && Uri.TryCreate(a.CodeBase, UriKind.Absolute, out var uri))
                {
                    var local = Uri.UnescapeDataString(uri.LocalPath);
                    var dir = Path.GetDirectoryName(local);
                    if (!string.IsNullOrEmpty(dir))
                        return dir;
                }
            }
            catch
            {
                // fall through
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
