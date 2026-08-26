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

        /// <summary>When true, this row is part of a replay plan (not global input-hook noise).</summary>
        public bool IsReplayPlanRow { get; set; }

        /// <summary>0-based index into the step list for replay highlighting.</summary>
        public int ReplayStepIndex { get; set; } = -1;

        /// <summary>Replay UI: pending | active | ok | error</summary>
        public string ReplayPhase { get; set; }

        /// <summary>When <see cref="ReplayPhase"/> is error, last failure message for this step (shown on the card).</summary>
        public string ReplayErrorMessage { get; set; }

        /// <summary>Skip JSONL logging for synthetic replay scaffold rows.</summary>
        public bool SuppressFileLog { get; set; }
    }
}
