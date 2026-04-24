using System;
using System.Collections.Generic;

namespace MARS.WebAutomation.Models
{
    public sealed class WebTestDocument
    {
        public string SchemaVersion { get; set; } = "1.0";
        public DateTime SavedAtUtc { get; set; }
        public PageInfoDto PageInfo { get; set; }
        public List<SemanticStepRecord> Steps { get; set; } = new List<SemanticStepRecord>();
        public List<NetworkCaptureEntry> NetworkCaptures { get; set; } = new List<NetworkCaptureEntry>();
        public Dictionary<string, string> SettingsSnapshot { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
