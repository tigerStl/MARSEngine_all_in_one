using System;
using System.Collections.Generic;

namespace MARS.WebAutomation.Models
{
    public sealed class NetworkCaptureEntry
    {
        public string Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string ResourceType { get; set; }
        public int? Status { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public Dictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ResponseHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string CookiesSummary { get; set; }
    }
}
