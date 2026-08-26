using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mars.AutoTestingDriver.Utils
{
    public class ExternalAgentConfig
    {
        public string AgentName { get; set; }
        public string ExePath { get; set; }
        public string ProcessName { get; set; }
        public string Arguments { get; set; }
        public string InvokeUrl { get; set; }
        public string HealthCheckUrl { get; set; }
        public int StartDelayMs { get; set; } = 3000;
        public bool UseHttp { get; set; } = true;
    }

    public static class ExternalAgentManager
    {
        public const string DEFAULT_AGENT_NAME = "MarsDefaultAgent";
        private static readonly string ConfigFileRelative = Path.Combine("config", "MarsExternalAgents.json");

        public static ExternalAgentConfig GetAgentConfig(string agentName= DEFAULT_AGENT_NAME)
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
                if (string.IsNullOrEmpty(cfg.ExePath) || !File.Exists(cfg.ExePath))
                {
                    error = $"Agent executable not found: {cfg.ExePath}";
                    return false;
                }
                var startInfo = new ProcessStartInfo
                {
                    FileName = cfg.ExePath,
                    Arguments = cfg.Arguments ?? string.Empty,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(cfg.ExePath)
                };
                Process.Start(startInfo);
                if (cfg.StartDelayMs > 0)
                    System.Threading.Thread.Sleep(cfg.StartDelayMs);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static async Task<string> InvokeAgentAsync(ExternalAgentConfig cfg, string action, string payload)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (cfg.UseHttp && !string.IsNullOrEmpty(cfg.InvokeUrl))
            {
                using (var http = new HttpClient())
                {
                    var obj = new { action = action, payload = payload };
                    var json = JsonConvert.SerializeObject(obj);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await http.PostAsync(cfg.InvokeUrl, content).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            // no invoke mechanism configured
            return null;
        }
    }
}
