using System;
using System.Text;
using System.Windows.Forms;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MarsRabbitMQServerSample
{
    public class MainForm : Form
    {
        private readonly TextBox _downQueueTextBox;
        private readonly TextBox _upQueueTextBox;

        private IConnection? _connection;
        private IModel? _downChannel;
        private IModel? _upChannel;

        private const string HostName = "127.0.0.1";
        private const int Port = 5672;
        private const string UserName = "guest";
        private const string Password = "guest";
        private const string VirtualHost = "/";

        private const string DownQueueName = "MARsserverDownQueue";
        private const string UpQueueName = "MARSServerUpperQueue";

        private readonly string _logFilePath;

        public MainForm()
        {
            Text = "Mars RabbitMQ Server Sample";
            Width = 900;
            Height = 600;

            _logFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.log");

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280
            };

            _downQueueTextBox = CreateTextBox();
            _upQueueTextBox = CreateTextBox();

            split.Panel1.Padding = new Padding(5);
            split.Panel2.Padding = new Padding(5);

            split.Panel1.Controls.Add(WrapWithGroup("Down Queue - MARsserverDownQueue", _downQueueTextBox));
            split.Panel2.Controls.Add(WrapWithGroup("Up Queue - MARSServerUpperQueue", _upQueueTextBox));

            Controls.Add(split);

            Shown += (_, _) => StartRabbitListeners();
            FormClosing += OnFormClosing;
        }

        private static TextBox CreateTextBox()
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9),
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.LightGreen
            };
        }

        private static Control WrapWithGroup(string title, Control inner)
        {
            var group = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill
            };
            inner.Parent = group;
            group.Controls.Add(inner);
            return group;
        }

        private void StartRabbitListeners()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = HostName,
                    Port = Port,
                    UserName = UserName,
                    Password = Password,
                    VirtualHost = VirtualHost
                };

                _connection = factory.CreateConnection();

                _downChannel = _connection.CreateModel();
                _upChannel = _connection.CreateModel();

                // 确保队列存在
                _downChannel.QueueDeclare(DownQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _upChannel.QueueDeclare(UpQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var downConsumer = new EventingBasicConsumer(_downChannel);
                downConsumer.Received += (_, ea) =>
                {
                    HandleMessage(DownQueueName, ea, _downQueueTextBox);
                    _downChannel.BasicAck(ea.DeliveryTag, multiple: false);
                };
                _downChannel.BasicConsume(DownQueueName, autoAck: false, consumer: downConsumer);

                var upConsumer = new EventingBasicConsumer(_upChannel);
                upConsumer.Received += (_, ea) =>
                {
                    HandleMessage(UpQueueName, ea, _upQueueTextBox);
                    _upChannel.BasicAck(ea.DeliveryTag, multiple: false);
                };
                _upChannel.BasicConsume(UpQueueName, autoAck: false, consumer: upConsumer);

                AppendLine(_downQueueTextBox, $"[{DateTime.Now:HH:mm:ss}] 监听队列 {DownQueueName} 中...");
                AppendLine(_upQueueTextBox, $"[{DateTime.Now:HH:mm:ss}] 监听队列 {UpQueueName} 中...");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"连接 RabbitMQ 失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleMessage(string queueName, BasicDeliverEventArgs ea, TextBox targetTextBox)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var line = $"[{now}] [{queueName}] {message}";

            AppendLine(targetTextBox, line);

            try
            {
                System.IO.File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppendLine(targetTextBox, $"写日志失败: {ex.Message}");
            }
        }

        private void AppendLine(TextBox box, string text)
        {
            if (box.InvokeRequired)
            {
                box.BeginInvoke(new Action<TextBox, string>(AppendLine), box, text);
                return;
            }

            box.AppendText(text + Environment.NewLine);
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                _downChannel?.Close();
                _downChannel?.Dispose();
                _upChannel?.Close();
                _upChannel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
            catch
            {
                // ignore shutdown errors
            }
        }
    }
}

