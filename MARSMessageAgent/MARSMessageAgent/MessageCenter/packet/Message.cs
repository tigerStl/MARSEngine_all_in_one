using System;

namespace MARS_WebCore.MQ.Models
{
    public record MQMessage<T>
    {
        //public Guid UUID { get; init; }

        public string Version { get; init; } = "1.0";

        public string Command { get; init; } = string.Empty;

        public DateTime? ExpireTime { get; init; }

        public T? JobEntity { get; init; }

        public MessageDirection Direction { get; init; }
    }

    public enum MessageDirection
    {
        FromServer,
        FromEngine
    }
     
    public enum TaskStatus
    {
        Processing = 0,
        DoneWithOk = 1,
        Failed = 2
    }

    public enum EngineStatus
    {
        Idle = 0,
        Busy = 1,
        Offline = 2
    }
    public record TimePolicy
    {
        public DateTime? StartTime { get; init; }

        public DateTime? EndTime { get; init; }
    }


    public static class MQCommands
    {
        public static class FromServer
        {
            public const string ExecuteStoryboardRequest = "ExecuteStoryboardRequest";
            public const string QueryExecutionStatusRequest = "QueryExecutionStatusRequest";
            public const string EngineRegistered = "EngineRegistered";
        }

        public static class FromEngine
        {
            public const string ExecuteStoryboardResponse = "ExecuteStoryboardResponse";
            public const string QueryExecutionStatusResponse = "QueryExecutionStatusResponse";
            public const string Heartbeat = "Heartbeat";
            public const string Register = "register";
        }
         

    }
}
