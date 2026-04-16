using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MARS_WebCore.MQ.Models;

namespace Mars.MessageCenter.RabbitClient
{
    /// <summary>
    /// 与远端 RabbitMQ Server 通信的客户端：
    /// - 从下行队列（例如 MARsserverDownQueue）接收 WebServer 下发的任务；
    /// - 向上行队列（例如 MARSServerUpperQueue）发送执行结果、Heartbeat、Register 等。
    /// </summary>
    public sealed class RabbitMqClient : IDisposable
    {
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IModel? _consumeChannel;
        private IModel? _publishChannel;
        private EventingBasicConsumer? _consumer;
        private string? _consumerTag;
        private bool _started;

        public RabbitMqClient(RabbitMqOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 收到 ExecuteStoryboardRequest 消息时触发。
        /// </summary>
        public event EventHandler<MQMessage<ExecuteStoryboardRequestEntity>>? ExecuteStoryboardRequestReceived;

        /// <summary>
        /// 收到 QueryExecutionStatusRequest 消息时触发。
        /// </summary>
        public event EventHandler<MQMessage<QueryExecutionStatusRequestEntity>>? QueryExecutionStatusRequestReceived;

        /// <summary>
        /// 收到 EngineRegistered 消息时触发（WebServer 确认注册）。
        /// </summary>
        public event EventHandler<MQMessage<EngineRegisteredResponseEntity>>? EngineRegisteredReceived;

        /// <summary>
        /// 收到无法识别的命令时触发，便于上层统一处理或记录日志。
        /// </summary>
        public event EventHandler<string>? UnknownCommandReceived;

        public void Start()
        {
            if (_started)
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };

            _connection = factory.CreateConnection();

            _consumeChannel = _connection.CreateModel();
            _publishChannel = _connection.CreateModel();

            // 确保队列存在
            _consumeChannel.QueueDeclare(queue: _options.DownQueueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

            _publishChannel.QueueDeclare(queue: _options.UpQueueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

            _consumer = new EventingBasicConsumer(_consumeChannel);
            _consumer.Received += OnConsumerReceived;

            _consumerTag = _consumeChannel.BasicConsume(queue: _options.DownQueueName,
                                         autoAck: false,
                                         consumer: _consumer);

            _started = true;
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            try
            {
                if (_consumeChannel != null && !string.IsNullOrEmpty(_consumerTag))
                {
                    _consumeChannel.BasicCancel(_consumerTag);
                }
            }
            catch
            {
                // 忽略关闭过程中的异常
            }

            _consumeChannel?.Close();
            _consumeChannel?.Dispose();
            _consumeChannel = null;

            _publishChannel?.Close();
            _publishChannel?.Dispose();
            _publishChannel = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;

            _started = false;
        }

        private void OnConsumerReceived(object? sender, BasicDeliverEventArgs e)
        {
            try
            {
                var json = Encoding.UTF8.GetString(e.Body.ToArray());
                var jObject = JObject.Parse(json);

                var command = jObject.Value<string>("Command") ?? string.Empty;

                switch (command)
                {
                    case MQCommands.FromServer.ExecuteStoryboardRequest:
                        HandleExecuteStoryboardRequest(jObject);
                        break;

                    case MQCommands.FromServer.QueryExecutionStatusRequest:
                        HandleQueryExecutionStatusRequest(jObject);
                        break;

                    case MQCommands.FromServer.EngineRegistered:
                        HandleEngineRegistered(jObject);
                        break;

                    default:
                        UnknownCommandReceived?.Invoke(this, json);
                        break;
                }

                _consumeChannel?.BasicAck(e.DeliveryTag, multiple: false);
            }
            catch
            {
                // 解析失败时，将消息标记为未确认并丢弃 / 死信。
                if (_consumeChannel != null)
                {
                    _consumeChannel.BasicNack(e.DeliveryTag, multiple: false, requeue: false);
                }
            }
        }

        private static MQMessage<T> BuildTypedMessage<T>(JObject root, T entity)
        {
            return new MQMessage<T>
            {
                Version = root.Value<string>("Version") ?? "1.0",
                Command = root.Value<string>("Command") ?? string.Empty,
                ExpireTime = root.Value<DateTime?>("ExpireTime"),
                JobEntity = entity,
                Direction = MessageDirection.FromServer
            };
        }

        private void HandleExecuteStoryboardRequest(JObject root)
        {
            var jobToken = root["JobEntity"];
            if (jobToken == null)
            {
                return;
            }

            var entity = jobToken.ToObject<ExecuteStoryboardRequestEntity>();
            if (entity == null)
            {
                return;
            }

            var message = BuildTypedMessage(root, entity);
            ExecuteStoryboardRequestReceived?.Invoke(this, message);
        }

        private void HandleQueryExecutionStatusRequest(JObject root)
        {
            var jobToken = root["JobEntity"];
            if (jobToken == null)
            {
                return;
            }

            var entity = jobToken.ToObject<QueryExecutionStatusRequestEntity>();
            if (entity == null)
            {
                return;
            }

            var message = BuildTypedMessage(root, entity);
            QueryExecutionStatusRequestReceived?.Invoke(this, message);
        }

        private void HandleEngineRegistered(JObject root)
        {
            var jobToken = root["JobEntity"];
            if (jobToken == null)
            {
                return;
            }

            var entity = jobToken.ToObject<EngineRegisteredResponseEntity>();
            if (entity == null)
            {
                return;
            }

            var message = BuildTypedMessage(root, entity);
            EngineRegisteredReceived?.Invoke(this, message);
        }

        /// <summary>
        /// 发送 Register 消息，表明当前 Agent/Engine 可用。
        /// </summary>
        public void SendRegister(string replyToQueue)
        {
            var entity = new RegisterEntity
            {
                IP = GetLocalIpAddress(),
                HostName = Dns.GetHostName(),
                ReplyTo = replyToQueue,
                Status = EngineStatus.Idle
            };

            var mq = new MQMessage<RegisterEntity>
            {
                Version = "1.0",
                Command = MQCommands.FromEngine.Register,
                ExpireTime = null,
                JobEntity = entity,
                Direction = MessageDirection.FromEngine
            };

            PublishToUpperQueue(mq);
        }

        /// <summary>
        /// 发送 Heartbeat 消息，保持与 WebServer 的心跳。
        /// </summary>
        public void SendHeartbeat(string engineId)
        {
            var entity = new HeartbeatEntity
            {
                EngineId = engineId,
                IP = GetLocalIpAddress(),
                HostName = Dns.GetHostName(),
                Status = EngineStatus.Idle,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var mq = new MQMessage<HeartbeatEntity>
            {
                Version = "1.0",
                Command = MQCommands.FromEngine.Heartbeat,
                ExpireTime = null,
                JobEntity = entity,
                Direction = MessageDirection.FromEngine
            };

            PublishToUpperQueue(mq);
        }

        /// <summary>
        /// 发送 ExecuteStoryboardResponse。
        /// </summary>
        public void SendExecuteStoryboardResponse(ExecuteResponseEntity response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var mq = new MQMessage<ExecuteResponseEntity>
            {
                Version = "1.0",
                Command = MQCommands.FromEngine.ExecuteStoryboardResponse,
                ExpireTime = null,
                JobEntity = response,
                Direction = MessageDirection.FromEngine
            };

            PublishToUpperQueue(mq);
        }

        /// <summary>
        /// 发送 QueryExecutionStatusResponse。
        /// </summary>
        public void SendQueryExecutionStatusResponse(ExecuteResponseEntity response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            var mq = new MQMessage<ExecuteResponseEntity>
            {
                Version = "1.0",
                Command = MQCommands.FromEngine.QueryExecutionStatusResponse,
                ExpireTime = null,
                JobEntity = response,
                Direction = MessageDirection.FromEngine
            };

            PublishToUpperQueue(mq);
        }

        private void PublishToUpperQueue<T>(MQMessage<T> message)
        {
            if (_publishChannel == null)
                throw new InvalidOperationException("RabbitMqClient is not started.");

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _publishChannel.CreateBasicProperties();
            props.DeliveryMode = 2; // 持久化

            _publishChannel.BasicPublish(exchange: string.Empty,
                                         routingKey: _options.UpQueueName,
                                         basicProperties: props,
                                         body: new ReadOnlyMemory<byte>(body));
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                var addresses = Dns.GetHostAddresses(hostName);
                foreach (var ip in addresses)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return "127.0.0.1";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

