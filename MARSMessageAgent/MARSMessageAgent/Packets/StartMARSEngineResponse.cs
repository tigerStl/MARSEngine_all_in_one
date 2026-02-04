using Newtonsoft.Json;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 启动 MARS Engine 响应：packageType = Start_MARSEngine_response
    /// </summary>
    public class StartMARSEngineResponse : PacketBase
    {
        public const string PackageTypeValue = "Start_MARSEngine_response";

        [JsonProperty("result")]
        public object Result { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        public StartMARSEngineResponse() : base(PackageTypeValue, null) { }

        public StartMARSEngineResponse(string sessionId, bool result, string message)
            : base(PackageTypeValue, sessionId)
        {
            Result = result;
            Message = message;
        }

        public StartMARSEngineResponse(string sessionId, string result, string message)
            : base(PackageTypeValue, sessionId)
        {
            Result = result;
            Message = message;
        }
    }
}
