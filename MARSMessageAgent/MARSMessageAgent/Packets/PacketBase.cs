using System;
using Newtonsoft.Json;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 所有功能包的基类。包含 packageType、SessionId、DateTime。
    /// </summary>
    [Serializable]
    public abstract class PacketBase
    {
        [JsonProperty("packageType")]
        public string PackageType { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("dateTime")]
        public string DateTime { get; set; }

        protected PacketBase()
        {
            DateTime = System.DateTime.UtcNow.ToString("o");
        }

        protected PacketBase(string packageType, string sessionId)
        {
            PackageType = packageType;
            SessionId = sessionId;
            DateTime = System.DateTime.UtcNow.ToString("o");
        }
    }
}
