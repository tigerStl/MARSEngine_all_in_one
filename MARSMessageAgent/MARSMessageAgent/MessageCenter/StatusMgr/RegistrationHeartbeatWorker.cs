using System;
using System.IO;
using System.Threading;
using Mars.MessageCenter.RabbitClient;
using Route2NSEx.src.Marquis.systemUtil;

namespace MARSMessageAgent.MessageCenter.StatusMgr
{
    /// <summary>
    /// 后台线程：读取 MARSMessageCenter.config.json，连接上行队列，注册后每分钟发送心跳；
    /// 连接失败则每 10 秒重试；发送异常或队列关闭则重新走注册流程并记录日志。
    /// </summary>
    public static class RegistrationHeartbeatWorker
    {
        private const string ConfigFileName = "MARSMessageCenter.config.json";
        private const int ConnectRetrySeconds = 10;
        private const int HeartbeatIntervalMs = 60 * 1000;

        private static volatile bool _stopRequested;
        private static MLogger _log;

        private static MLogger Log
        {
            get
            {
                if (_log == null)
                {
                    MLogger.LogFileName = "MessageCenterLog";
                    _log = MLogger.GetLogger(typeof(RegistrationHeartbeatWorker));
                }
                return _log;
            }
        }

        public static void RunLoop()
        {
            _stopRequested = false;
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

            while (!_stopRequested)
            {
                RabbitMqOptions options = null;
                try
                {
                    if (!File.Exists(configPath))
                    {
                        Log?.Error(nameof(RunLoop), "Config file not found: " + configPath);
                        Thread.Sleep(ConnectRetrySeconds * 1000);
                        continue;
                    }

                    options = RabbitMqOptions.Load(configPath);
                }
                catch (Exception ex)
                {
                    Log?.Error(nameof(RunLoop), "Load config failed: " + ex.Message, ex);
                    Thread.Sleep(ConnectRetrySeconds * 1000);
                    continue;
                }

                using (var client = new RabbitMqClient(options))
                {
                    string engineId = null;
                    client.EngineRegisteredReceived += (s, msg) =>
                    {
                        if (msg?.JobEntity != null)
                            engineId = msg.JobEntity.EngineId;
                    };

                    try
                    {
                        Log?.logBegin(nameof(RunLoop), "Connecting to " + options.HostName + " UpQueue=" + options.UpQueueName);
                        client.Start();
                        Log?.logBegin("SendRegister", options.UpQueueName);
                        client.SendRegister(options.UpQueueName);
                    }
                    catch (Exception ex)
                    {
                        Log?.Error(nameof(RunLoop), "Connect or Register failed: " + ex.Message, ex);
                        Thread.Sleep(ConnectRetrySeconds * 1000);
                        continue;
                    }

                    int nextHeartbeatMs = HeartbeatIntervalMs;
                    while (!_stopRequested)
                    {
                        int sleepMs = Math.Min(1000, nextHeartbeatMs);
                        Thread.Sleep(sleepMs);
                        nextHeartbeatMs -= sleepMs;

                        if (nextHeartbeatMs <= 0)
                        {
                            nextHeartbeatMs = HeartbeatIntervalMs;
                            try
                            {
                                string id = engineId ?? System.Net.Dns.GetHostName();
                                client.SendHeartbeat(id);
                            }
                            catch (Exception ex)
                            {
                                Log?.Error("SendHeartbeat", "Heartbeat failed: " + ex.Message, ex);
                                break;
                            }
                        }
                    }
                }

                if (!_stopRequested)
                    Thread.Sleep(ConnectRetrySeconds * 1000);
            }
        }

        public static void Stop()
        {
            _stopRequested = true;
        }
    }
}
