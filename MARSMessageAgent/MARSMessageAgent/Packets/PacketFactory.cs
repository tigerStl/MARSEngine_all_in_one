using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MARSMessageAgent.Packets
{
    /// <summary>
    /// 将 JSON 根据 packageType 反序列化为对应的包类型。
    /// 统一使用 Newtonsoft.Json。
    /// </summary>
    public static class PacketFactory
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        /// <summary>
        /// 将 JSON 字符串反序列化为对应的请求/响应包类型。
        /// </summary>
        public static PacketBase FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string is null or empty.", nameof(json));

            var obj = JObject.Parse(json);
            var packageType = obj["packageType"]?.ToString();

            if (string.IsNullOrEmpty(packageType))
                throw new ArgumentException("JSON must contain 'packageType'.", nameof(json));

            switch (packageType)
            {
                case ShakeHandRequest.PackageTypeValue:
                    return JsonConvert.DeserializeObject<ShakeHandRequest>(json, JsonSettings);
                case ShakeHandResponse.PackageTypeValue:
                    return JsonConvert.DeserializeObject<ShakeHandResponse>(json, JsonSettings);
                case StartMARSEngineRequest.PackageTypeValue:
                    return JsonConvert.DeserializeObject<StartMARSEngineRequest>(json, JsonSettings);
                case StartMARSEngineResponse.PackageTypeValue:
                    return JsonConvert.DeserializeObject<StartMARSEngineResponse>(json, JsonSettings);
                default:
                    throw new NotSupportedException($"Unknown packageType: {packageType}");
            }
        }

        /// <summary>
        /// 将对象序列化为 JSON。
        /// </summary>
        public static string ToJson(PacketBase packet)
        {
            return JsonConvert.SerializeObject(packet, Formatting.None, JsonSettings);
        }
    }
}
