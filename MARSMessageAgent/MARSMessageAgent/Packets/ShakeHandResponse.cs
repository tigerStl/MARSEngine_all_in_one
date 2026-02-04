using Newtonsoft.Json;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 握手响应：packageType = ShakeHandle_response, message = OK
    /// </summary>
    public class ShakeHandResponse : PacketBase
    {
        public const string PackageTypeValue = "ShakeHandle_response";

        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Agent's WebSocket server port; sent to Driver so it knows where to connect.
        /// </summary>
        [JsonProperty("wsServerPort")]
        public int WsServerPort { get; set; }

        public ShakeHandResponse() : base(PackageTypeValue, null)
        {
            Message = "OK";
        }

        public ShakeHandResponse(string sessionId, string message = "OK")
            : base(PackageTypeValue, sessionId)
        {
            Message = message;
        }

        public ShakeHandResponse(string sessionId, string message, int wsServerPort)
            : base(PackageTypeValue, sessionId)
        {
            Message = message ?? "OK";
            WsServerPort = wsServerPort;
        }
    }
}
