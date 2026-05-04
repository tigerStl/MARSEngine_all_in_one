using MARS.WebAutomation.Services;

namespace MARS.WebAutomation.Performance.ExecuteAdapter.NBomberInterface
{
    /// <summary>Optional telemetry for a single NBomber execution (response logging, validation, live metrics).</summary>
    public sealed class NBomberTelemetryContext
    {
        public bool SaveResponseBodies { get; set; }

        /// <summary>Directory to write per-response files (e.g. <c>...\data\test\log</c>).</summary>
        public string ResponseLogDirectory { get; set; }

        /// <summary>If non-empty: when status is 200–299, response body must contain this substring (ordinal ignore case).</summary>
        public string ResponseBodyMustContain { get; set; }

        public PerformanceMetricsCollector Metrics { get; set; }
    }
}
