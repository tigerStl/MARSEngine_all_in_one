using System;
using System.Collections.Generic;

namespace MARS.WebAutomation.Models
{
    /// <summary>Open JSON interchange for performance anchors and optional NBomber transaction tuning.</summary>
    public sealed class PerformancePackDocument
    {
        /// <summary>Format id, e.g. <c>mars.perf-pack/1.0</c>.</summary>
        public string SchemaVersion { get; set; } = "mars.perf-pack/1.0";

        public DateTime ExportedAtUtc { get; set; }

        /// <summary>Optional page URL this pack was captured against.</summary>
        public string SourcePageUrl { get; set; }

        public int? DefaultSimUsers { get; set; }

        public int? DefaultDurationSeconds { get; set; }

        public string Notes { get; set; }

        public List<PerformanceRequestRecord> Requests { get; set; } = new List<PerformanceRequestRecord>();

        /// <summary>Per AnchorGroup (transaction name) tuning for NBomber.</summary>
        public Dictionary<string, PerformanceTransactionConfigEntry> TransactionConfig { get; set; } =
            new Dictionary<string, PerformanceTransactionConfigEntry>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class PerformanceTransactionConfigEntry
    {
        public bool Enabled { get; set; } = true;

        public int? UsersOverride { get; set; }

        public int? DurationSecondsOverride { get; set; }

        public int Weight { get; set; } = 1;
    }
}
