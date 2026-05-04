using System;
using System.Collections.Generic;

namespace MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface
{
    public sealed class NBomberExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime CompletedUtc { get; set; }
        public int SimulatedUsers { get; set; }
        public long TotalOk { get; set; }
        public long TotalFail { get; set; }
        public List<string> ExecutedTransactions { get; set; } = new List<string>();
    }

    public sealed class NBomberProgressSnapshot
    {
        public DateTime TimestampUtc { get; set; }
        public string Stage { get; set; }
        public string Transaction { get; set; }
        public string StepName { get; set; }
        public long TotalStarted { get; set; }
        public long TotalOk { get; set; }
        public long TotalFail { get; set; }
        public int SimulatedUsers { get; set; }
        public string Detail { get; set; }
    }
}
