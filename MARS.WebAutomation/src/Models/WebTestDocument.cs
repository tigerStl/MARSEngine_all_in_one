using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MARS.WebAutomation.Models
{
    public sealed class WebTestDocument
    {
        public string SchemaVersion { get; set; } = "1.0";
        public DateTime SavedAtUtc { get; set; }
        public PageInfoDto PageInfo { get; set; }
        public List<SemanticStepRecord> Steps { get; set; } = new List<SemanticStepRecord>();
        public List<ObjectTreeNodeDto> ObjectTreeRoots { get; set; } = new List<ObjectTreeNodeDto>();
        public List<NetworkCaptureEntry> NetworkCaptures { get; set; } = new List<NetworkCaptureEntry>();
        public List<PerformanceRequestRecord> PerformanceRequests { get; set; } = new List<PerformanceRequestRecord>();
        public int? PerformanceDefaultSimUsers { get; set; }
        public int? PerformanceDefaultDurationSeconds { get; set; }
        public Dictionary<string, PerformanceTransactionConfigEntry> PerformanceTransactionConfig { get; set; } =
            new Dictionary<string, PerformanceTransactionConfigEntry>(StringComparer.OrdinalIgnoreCase);
        public JToken PerformanceResult { get; set; }
        public Dictionary<string, string> SettingsSnapshot { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
