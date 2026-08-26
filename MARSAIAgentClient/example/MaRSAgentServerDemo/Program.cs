using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MARSAIAgentClient;

namespace MaRSAgentServerDemo
{
    // Very small in-process agent server used for demo purposes
    class AgentServer
    {
        private readonly ConcurrentDictionary<string, Func<string, string, Task<string>>> _handlers = new();

        public void Register(string agentName, string methodName, Func<string, string, Task<string>> handler)
        {
            var key = GetKey(agentName, methodName);
            _handlers[key] = handler;
        }

        public Task<string> InvokeAsync(string agentName, string methodName, string payload)
        {
            var key = GetKey(agentName, methodName);
            if (_handlers.TryGetValue(key, out var handler))
            {
                return handler(methodName, payload);
            }
            return Task.FromResult<string>($"Method {methodName} not found on agent {agentName}");
        }

        private static string GetKey(string agentName, string methodName) => $"{agentName}::{methodName}";
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("MaRS Agent Server Demo");

            var server = new AgentServer();

            // Register a simple agent and method
            server.Register("demo-agent", "SayHello", async (method, payload) =>
            {
                await Task.Yield();
                return $"Hello, {payload}! (from {method})";
            });

            // Configure client. If program started with "http" argument, use HTTP transport to remote server.
            var useHttp = args != null && args.Length > 0 && args[0] == "http";
            var config = new AgentConfig { AgentName = "demo-agent", Endpoint = useHttp ? "http://localhost:5000" : "inprocess://localhost" };
            var client = new AgentClient(config);

            if (useHttp)
            {
                // Use HTTP invoker to call remote agent server
                client.Configure(AgentHttpInvoker.CreateInvoker(config.Endpoint));
            }
            else
            {
                // Provide the client with a transport delegate that calls the in-process server
                client.Configure(async (agentName, methodName, payload) =>
                {
                    return await server.InvokeAsync(agentName, methodName, payload).ConfigureAwait(false);
                });
            }

            // Call the agent method
            var result = await client.InvokeAsync("SayHello", "MaRS");
            Console.WriteLine("Agent response: " + result);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
