using System.Threading.Tasks;

namespace MARSAIAgentClient
{
    public interface IAgentClient
    {
        // Configure the invoker that will be used to send requests to an agent implementation
        void Configure(System.Func<string, string, string, Task<string>> invoker);

        // Invoke a method on the configured agent
        Task<string> InvokeAsync(string method, string payload);
    }
}
