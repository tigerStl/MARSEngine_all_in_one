using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MARSAIAgentClient
{
    // Provides a factory for HTTP-based invoker delegates used by AgentClient.
    public static class AgentHttpInvoker
    {
        public static Func<string, string, string, Task<string>> CreateInvoker(string baseEndpoint)
        {
            if (string.IsNullOrWhiteSpace(baseEndpoint)) throw new ArgumentNullException(nameof(baseEndpoint));
            var http = new HttpClient { BaseAddress = new Uri(baseEndpoint) };

            return async (agentName, methodName, payload) =>
            {
                // Simple JSON payload
                var json = $"{{\"agentName\":\"{Escape(agentName)}\",\"methodName\":\"{Escape(methodName)}\",\"payload\":\"{Escape(payload)}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                // POST to /invoke (user's agent server should accept this contract)
                var resp = await http.PostAsync("/invoke", content).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            };
        }

        private static string Escape(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
