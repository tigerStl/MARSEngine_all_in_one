using System;
using System.Collections.Generic;

namespace MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface
{
    public sealed class NBomberExecutionPlan
    {
        public string TestSuite { get; set; } = "MARS.WebAutomation";
        public string TestName { get; set; } = "RecordedPerformanceRun";
        public int SimulatedUsers { get; set; } = 5;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);
        public int RampUpSeconds { get; set; } = 5;
        public string ReportFolder { get; set; } = "nbomber-reports";
        public bool WithoutReports { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public List<NBomberTransactionPlan> Transactions { get; set; } = new List<NBomberTransactionPlan>();

        /// <summary>Optional: response logging, body validation, live metrics.</summary>
        public NBomberTelemetryContext Telemetry { get; set; }
    }

    public sealed class NBomberTransactionPlan
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public int Weight { get; set; } = 1;
        public int? SimulatedUsersOverride { get; set; }
        public TimeSpan? DurationOverride { get; set; }
        public List<NBomberRequestStep> Steps { get; set; } = new List<NBomberRequestStep>();
    }

    public sealed class NBomberRequestStep
    {
        public string Name { get; set; }
        public string Method { get; set; } = "GET";
        public string Url { get; set; }
        public string Headers { get; set; }
        public string Payload { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string ExpectedStatusCodes { get; set; } = "200-399";
        public int TimeoutMs { get; set; } = 30000;
        public bool Skip { get; set; }
    }
}
