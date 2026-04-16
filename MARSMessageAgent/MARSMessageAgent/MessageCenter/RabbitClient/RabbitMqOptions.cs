using System;
using System.IO;
using Newtonsoft.Json;

namespace Mars.MessageCenter.RabbitClient
{
    /// <summary>
    /// RabbitMQ 连接与队列配置。
    /// 从应用根目录下的 MARSMessageCenter.config.json 读取（不含用户名密码，连接时使用默认 guest）。
    /// </summary>
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// WebServer 下发任务的队列名（下行队列），默认 MARsserverDownQueue。
        /// </summary>
        public string DownQueueName { get; set; } = "MARsserverDownQueue";

        /// <summary>
        /// Agent 上传执行结果的队列名（上行队列），默认 MARSServerUpperQueue。
        /// </summary>
        public string UpQueueName { get; set; } = "MARSServerUpperQueue";

        public static string GetDefaultConfigPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MARSMessageCenter.config.json");
        }

        public static RabbitMqOptions Load(string? configPath = null)
        {
            configPath ??= GetDefaultConfigPath();

            if (!File.Exists(configPath))
            {
                // 文件不存在时，返回默认配置；外部可以选择写出示例文件。
                return new RabbitMqOptions();
            }

            var json = File.ReadAllText(configPath);
            var options = JsonConvert.DeserializeObject<RabbitMqOptions>(json);
            return options ?? new RabbitMqOptions();
        }
    }
}

