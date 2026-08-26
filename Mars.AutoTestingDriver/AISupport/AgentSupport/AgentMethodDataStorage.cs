using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.AISupport.AgentSupport
{
    public static class AgentMethodDataStorage
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AgentMethodEntry> _store
            = new System.Collections.Concurrent.ConcurrentDictionary<string, AgentMethodEntry>(StringComparer.OrdinalIgnoreCase);

        private class AgentMethodEntry
        {
            public string AgentName { get; set; }
            public string MethodName { get; set; }
            public string Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private static string MakeKey(string agentName, string methodName)
        {
            return (agentName ?? "") + "::" + (methodName ?? "");
        }

        public static void SetMethodData(string agentName, string methodName, string data)
        {
            var key = MakeKey(agentName, methodName);
            var entry = new AgentMethodEntry
            {
                AgentName = agentName,
                MethodName = methodName,
                Data = data,
                Timestamp = DateTime.Now
            };
            _store.AddOrUpdate(key, entry, (k, old) => entry);
        }

        public static string GetMethodData(string agentName, string methodName)
        {
            var key = MakeKey(agentName, methodName);
            if (_store.TryGetValue(key, out var entry))
                return entry.Data;
            return null;
        }

        public static DateTime? GetMethodDataTimestamp(string agentName, string methodName)
        {
            var key = MakeKey(agentName, methodName);
            if (_store.TryGetValue(key, out var entry))
                return entry.Timestamp;
            return null;
        }
    }
}
