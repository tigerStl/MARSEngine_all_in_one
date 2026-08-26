using System;
using System.Threading.Tasks;

namespace MARSAIAgentClient
{
    public class AgentClient : IAgentClient
    {
        private readonly AgentConfig _config;
        private Func<string, string, string, Task<string>> _invoker;

        public AgentClient(AgentConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Configure(Func<string, string, string, Task<string>> invoker)
        {
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        }

        public async Task<string> InvokeAsync(string method, string payload)
        {
            if (_invoker == null) throw new InvalidOperationException("AgentClient invoker is not configured.");
            return await _invoker(_config.AgentName, method, payload).ConfigureAwait(false);
        }
    }
}
