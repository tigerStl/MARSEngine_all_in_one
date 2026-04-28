using System;

namespace MARS.WebAutomation.Models
{
    public sealed class RecordReplayEventCard
    {
        public int Sequence { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string EventName { get; set; }
        public string Position { get; set; }
        public string ObjectType { get; set; }
        public string Tag { get; set; }
        public string DataAttributes { get; set; }
        public string Xpath { get; set; }
        public string Value { get; set; }
        public string Id { get; set; }
        public string AriaAttributes { get; set; }
        public string Data { get; set; }
        public string ListenedRequestUrl { get; set; }
        public string ListenedRequestHeaders { get; set; }
        public string ExpectedResponse { get; set; }
    }
}
