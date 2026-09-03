using Mars.AutoTestingDriver.AISupport.AgentSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.Utils
{
    public class ExternalAgentInvokeUrl
    {
        public string Protocol { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }

        public bool IsConfigured
        {
            get { return !string.IsNullOrWhiteSpace(Protocol) && !string.IsNullOrWhiteSpace(Host); }
        }

        /// <summary>
        /// Builds the request URL. When a keyword is provided, the path is the keyword name in lowercase
        /// (e.g. activeprocess, snapshot, closeprocess, openexternalfile).
        /// </summary>
        public string ToUrl(string keyword = null)
        {
            if (!IsConfigured) return null;

            string scheme = Protocol.Trim();
            string host = Host.Trim();
            int port = Port;

            string path;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                path = "/invoke?action=" + keyword.Trim().Trim('/').ToLowerInvariant();
            }
            else
            {
                path = string.IsNullOrWhiteSpace(Path) ? "" : Path.Trim();
                if (!string.IsNullOrEmpty(path) && !path.StartsWith("/"))
                    path = "/" + path;
            }

            if (port > 0)
                return $"{scheme}://{host}:{port}{path}";
            return $"{scheme}://{host}{path}";
        }
    }

    public class ExternalAgentConfig
    {
        public string AgentName { get; set; }
        public string ExePath { get; set; }
        public string ProcessName { get; set; }
        public string Arguments { get; set; }
        public ExternalAgentInvokeUrl InvokeUrl { get; set; }
        public int StartDelayMs { get; set; } = 3000;
        public bool UseHttp { get; set; } = true;

        public bool CanInvokeHttp
        {
            get { return UseHttp && InvokeUrl != null && InvokeUrl.IsConfigured; }
        }

        public string GetActionUrl(string keyword)
        {
            return InvokeUrl?.ToUrl(keyword);
        }
    }

    public static class ExternalAgentManager
    {
        //public const string DEFAULT_AGENT_NAME = "MarsDefaultAgent";
        private static readonly string ConfigFileRelative = Path.Combine("config", "MarsExternalAgents.json");

        public static ExternalAgentConfig GetAgentConfig(string agentName= AgentHelper.CNST_AGENT_PARAMETER_DEFAULTAGENT)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // try several candidate locations: baseDir/config, parent dirs up to 6 levels
                string configPath = null;
                for (int i = 0; i < 6; i++)
                {
                    var candidate = Path.Combine(baseDir, ConfigFileRelative);
                    if (File.Exists(candidate))
                    {
                        configPath = candidate;
                        break;
                    }
                    baseDir = Path.GetDirectoryName(baseDir) ?? baseDir;
                }
                if (string.IsNullOrEmpty(configPath)) return null;
                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var list = JsonConvert.DeserializeObject<List<ExternalAgentConfig>>(json);
                return list?.FirstOrDefault(a => string.Equals(a.AgentName, agentName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        public static bool IsAgentRunning(ExternalAgentConfig cfg)
        {
            if (cfg == null) return false;
            string procName = !string.IsNullOrEmpty(cfg.ProcessName) ? cfg.ProcessName : (string.IsNullOrEmpty(cfg.ExePath) ? null : Path.GetFileNameWithoutExtension(cfg.ExePath));
            if (string.IsNullOrEmpty(procName)) return false;
            try
            {
                var procs = Process.GetProcessesByName(procName);
                return procs != null && procs.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartAgent(ExternalAgentConfig cfg, out string error)
        {
            error = null;
            if (cfg == null)
            {
                error = "agent config is null";
                return false;
            }
            try
            {
                string exePath = ResolveExePath(cfg.ExePath);
                if (string.IsNullOrEmpty(exePath))
                {
                    error = "Agent executable path is empty.";
                    return false;
                }

                if (!File.Exists(exePath))
                {
                    error = $"Agent executable not found: {exePath}";
                    return false;
                }

                int port = FindAvailablePort(cfg.InvokeUrl.Port, 200);
                    /*(cfg.InvokeUrl != null && cfg.InvokeUrl.Port > 0)
                    ? cfg.InvokeUrl.Port
                    : FindAvailablePort(10000, 200);
                    */
                if (port <= 0)
                {
                    error = "Cannot find available port for agent.";
                    return false;
                }

                var args = (cfg.Arguments ?? string.Empty).Trim();
                if (args.IndexOf("-port", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    if (!string.IsNullOrEmpty(args)) args = args + " ";
                    args = args + $"-port {port}";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exePath)
                };

                // Start the agent on a background thread, then wait until it binds the port
                // before continuing so subsequent HTTP invoke calls can succeed.
                Process proc = null;
                Exception startException = null;
                var timeoutMs = Math.Max(10000, cfg.StartDelayMs);
                var startThread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        proc = Process.Start(startInfo);
                    }
                    catch (Exception ex)
                    {
                        startException = ex;
                    }
                });
                startThread.IsBackground = true;
                startThread.Start();
                Thread.Sleep(timeoutMs);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool bound = false;
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    if (startException != null)
                        break;

                    if (proc != null && proc.HasExited)
                    {
                        error = $"Agent process exited immediately after start (exit code {proc.ExitCode}).";
                        return false;
                    }

                    // Port is no longer available => agent process has bound it.
                    if (proc != null && !IsPortAvailable(port))
                    {
                        bound = true;
                        break;
                    }

                    System.Threading.Thread.Sleep(200);
                }

                if (startException != null)
                {
                    error = startException.Message;
                    return false;
                }

                if (proc == null)
                {
                    error = "Failed to start agent process.";
                    return false;
                }

                //if (!bound)
                //{
                //    error = $"Agent started but did not bind to port {port} within {timeoutMs}ms.";
                //    return false;
                //}

                if (cfg.UseHttp && (cfg.InvokeUrl == null || !cfg.InvokeUrl.IsConfigured))
                {
                    cfg.InvokeUrl = new ExternalAgentInvokeUrl
                    {
                        Protocol = "http",
                        Host = "localhost",
                        Port = port,
                        Path = "/invoke"
                    };
                }
                else if (cfg.InvokeUrl != null && cfg.InvokeUrl.Port <= 0)
                {
                    cfg.InvokeUrl.Port = port;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string ResolveExePath(string exePath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(exePath))
                return Path.Combine(baseDir, "MARSAgent", "MarsDefaultAgent.exe");

            if (Path.IsPathRooted(exePath))
                return exePath;

            return Path.GetFullPath(Path.Combine(baseDir, exePath));
        }

        private static int FindAvailablePort(int startPort, int maxTry)
        {
            for (int i = 0; i < maxTry; i++)
            {
                int port = startPort + i;
                if (IsPortAvailable(port))
                    return port;
            }
            return -1;
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Invokes the agent HTTP endpoint for the given keyword. The URL path is the keyword name in lowercase.
        /// payload is sent as a JSON object (JSON strings are parsed; other values are serialized as objects).
        /// </summary>
        public static async Task<string> InvokeAgentAsync(ExternalAgentConfig cfg, string action, object payload)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (cfg.CanInvokeHttp)
            {
                string url = cfg.GetActionUrl(action);
                if (string.IsNullOrEmpty(url))
                    return null;

                using (var http = new HttpClient())
                {
                    var obj = new { action = action?.ToLowerInvariant(), payload = ToJsonPayload(payload) };
                    var json = JsonConvert.SerializeObject(obj);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await http.PostAsync(url, content).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            return null;
        }

        private static object ToJsonPayload(object payload)
        {
            if (payload == null)
                return null;
            if (payload is JToken)
                return payload;
            if (payload is string payloadStr)
            {
                if (string.IsNullOrWhiteSpace(payloadStr))
                    return payloadStr;
                try
                {
                    return JToken.Parse(payloadStr);
                }
                catch (JsonReaderException)
                {
                    return payloadStr;
                }
            }
            return payload;
        }
    }
}
