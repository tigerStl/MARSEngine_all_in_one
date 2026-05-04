using System;

namespace MARS.WebAutomation.Models
{
    public sealed class PerformanceRequestRecord
    {
        public string Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Action { get; set; }
        public string Method { get; set; }
        public string ResourceType { get; set; }
        public string Url { get; set; }
        public string Parameter { get; set; }
        public string Headers { get; set; }
        public string Cookies { get; set; }
        public string Payload { get; set; }
        public string Response { get; set; }
        public int? Status { get; set; }
        public string ReplayPolicy { get; set; }
        public string ValidationHint { get; set; }
        public int AnchorScore { get; set; }
        public bool AnchorCandidate { get; set; }
        public bool IsAnchorSelected { get; set; }
        public string AnchorGroup { get; set; }
        public bool CorrelationNeeded { get; set; }
        public string CorrelationHint { get; set; }
        public string Notes { get; set; }
        public string FilterTag { get; set; }
        public bool IsFiltered { get; set; }
    }
}
