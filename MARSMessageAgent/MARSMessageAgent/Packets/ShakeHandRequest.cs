using Newtonsoft.Json;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 握手请求：packageType = ShakeHandle_request
    /// </summary>
    public class ShakeHandRequest : PacketBase
    {
        public const string PackageTypeValue = "ShakeHandle_request";

        public ShakeHandRequest() : base(PackageTypeValue, null) { }

        [JsonConstructor]
        public ShakeHandRequest(string sessionId, string dateTime)
        {
            PackageType = PackageTypeValue;
            SessionId = sessionId;
            DateTime = dateTime ?? System.DateTime.UtcNow.ToString("o");
        }
    }
}
