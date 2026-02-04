using Newtonsoft.Json;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 启动 MARS Engine 请求：packageType = Start_MARSEngine_request
    /// </summary>
    public class StartMARSEngineRequest : PacketBase
    {
        public const string PackageTypeValue = "Start_MARSEngine_request";

        public StartMARSEngineRequest() : base(PackageTypeValue, null) { }

        [JsonConstructor]
        public StartMARSEngineRequest(string sessionId, string dateTime)
        {
            PackageType = PackageTypeValue;
            SessionId = sessionId;
            DateTime = dateTime ?? System.DateTime.UtcNow.ToString("o");
        }
    }
}
