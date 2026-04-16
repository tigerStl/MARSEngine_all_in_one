using System;
using Newtonsoft.Json.Linq;

namespace MARS_WebCore.MQ.Models
{
    public record ExecuteResponseEntity
    {

        public string UUId { get; init; } = string.Empty;
        public string TaskId { get; init; } = string.Empty;

        public TaskStatus Status { get; init; }

        public string? Message { get; init; }

        public JToken? Result { get; init; }
    }

    public record QueryExecutionStatusRequestEntity
    {
        public string UUId { get; init; } = string.Empty;
        public string TaskId { get; init; } = string.Empty;
    }

    public record ExecuteStoryboardRequestEntity
    {
        public string UUId { get; init; } = string.Empty;
        public string StoryboardId { get; init; } = string.Empty;

        public int DBIndex { get; init; }

        public TimePolicy? TimePolicy { get; init; }

        //public int? TargetEngineId { get; init; }

        public string TaskId { get; init; } = string.Empty;
    }

    public record BaseEngineEntity
    {
        public EngineStatus Status { get; set; } = EngineStatus.Idle;
        public string IP { get; init; } = string.Empty;
        public string HostName { get; init; } = string.Empty;
      
    }

    public record HeartbeatEntity: BaseEngineEntity
    {
        public string EngineId { get; init; } = string.Empty;
        public long Timestamp { get; init; }
    }

    public record RegisterEntity: BaseEngineEntity
    { 
        public string ReplyTo { get; init; } = string.Empty;
    }

    public record EngineRegisteredResponseEntity : RegisterEntity
    {
        public string EngineId { get; init; } = string.Empty;
    } 

    public record EngineInfo: BaseEngineEntity
    {
        public string EngineId { get; init; } = string.Empty;

        public string QueueName { get; set; } = string.Empty;


        public DateTime RegisterTime { get; init; }

        public DateTime LastHeartbeat { get; set; }
    }
}
