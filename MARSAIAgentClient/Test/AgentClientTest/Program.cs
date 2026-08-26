using System;
using System.Threading.Tasks;
using MARSAIAgentClient;

namespace AgentClientTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("AgentClientTest starting...");

            // Setup a simple in-process server similar to the demo
            var server = new SimpleTestServer();
            server.Register("server-agent", "Add", async (method, payload) =>
            {
                // payload expected as "a,b"
                await Task.Yield();
                var parts = payload?.Split(',');
                if (parts == null || parts.Length != 2) return "invalid payload";
                if (!int.TryParse(parts[0], out var a) || !int.TryParse(parts[1], out var b)) return "invalid numbers";
                return (a + b).ToString();
            });

            // Optionally use HTTP transport when started with the "http" argument
            var useHttp = args != null && args.Length > 0 && args[0] == "http";
            var config = new AgentConfig { AgentName = "server-agent", Endpoint = useHttp ? "http://localhost:5000" : "inprocess://localhost" };
            var client = new AgentClient(config);
            if (useHttp)
            {
                client.Configure(AgentHttpInvoker.CreateInvoker(config.Endpoint));
            }
            else
            {
                client.Configure((agentName, methodName, payload) => server.InvokeAsync(agentName, methodName, payload));
            }

            var response = await client.InvokeAsync("Add", "6,7");
            Console.WriteLine($"Invoke result: {response}");
            Console.WriteLine("Test complete. Press any key to exit.");
            Console.ReadKey();
        }
    }

    // Minimal server used by the test program
    class SimpleTestServer
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<string, string, Task<string>>> _map = new();

        public void Register(string agentName, string methodName, Func<string, string, Task<string>> handler)
        {
            var key = GetKey(agentName, methodName);
            _map[key] = handler;
        }

        public Task<string> InvokeAsync(string agentName, string methodName, string payload)
        {
            var key = GetKey(agentName, methodName);
            if (_map.TryGetValue(key, out var h)) return h(methodName, payload);
            return Task.FromResult("method-not-found");
        }

        private static string GetKey(string a, string m) => $"{a}::{m}";
    }
}
