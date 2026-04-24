using System;

namespace MARS.WebAutomation.Models
{
    public sealed class SemanticStepRecord
    {
        public DateTime TimestampUtc { get; set; }
        public string SourceEvent { get; set; }
        public string Keyword { get; set; }
        public string Locator { get; set; }
        public string Parameter { get; set; }
        public string Data { get; set; }
        public BoundingRectDto BoundingRect { get; set; }
    }
}
