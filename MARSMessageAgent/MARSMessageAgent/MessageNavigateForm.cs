using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Mars.MessageCenter.RabbitClient;
using MARS_WebCore.MQ.Models;

namespace MARSMessageAgent
{
    public class MessageNavigateForm : Form
    {
        private readonly TextBox _statusBox;
        private readonly TextBox _engineIdBox;
        private readonly Button _connectButton;
        private readonly Button _heartbeatButton;
        private readonly Button _sendOkButton;

        private RabbitMqClient _client;
        private RabbitMqOptions _options;
        private bool _connected;

        public MessageNavigateForm()
        {
            Text = "Message Navigate";
            Width = 700;
            Height = 450;

            Icon = LoadExeIconFromPng() ?? Icon;

            var engineIdLabel = new Label
            {
                Text = "EngineId:",
                Left = 10,
                Top = 15,
                AutoSize = true
            };

            _engineIdBox = new TextBox
            {
                Left = 80,
                Top = 10,
                Width = 200,
                Text = "engine-1"
            };

            _connectButton = new Button
            {
                Text = "Connect && Register",
                Left = 300,
                Top = 8,
                Width = 130
            };
            _connectButton.Click += (s, e) => EnsureConnectedAndRegister();

            _heartbeatButton = new Button
            {
                Text = "Send Heartbeat",
                Left = 440,
                Top = 8,
                Width = 110
            };
            _heartbeatButton.Click += (s, e) => SendHeartbeat();

            _sendOkButton = new Button
            {
                Text = "Send Test OK",
                Left = 560,
                Top = 8,
                Width = 110
            };
            _sendOkButton.Click += (s, e) => SendTestOk();

            _statusBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Left = 10,
                Top = 50,
                Width = ClientSize.Width - 20,
                Height = ClientSize.Height - 60,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(engineIdLabel);
            Controls.Add(_engineIdBox);
            Controls.Add(_connectButton);
            Controls.Add(_heartbeatButton);
            Controls.Add(_sendOkButton);
            Controls.Add(_statusBox);

            Load += MessageNavigateForm_Load;
            FormClosing += MessageNavigateForm_FormClosing;
        }

        private void MessageNavigateForm_Load(object sender, EventArgs e)
        {
            AppendStatus("Message Navigate ready.");
        }

        private void EnsureClient()
        {
            if (_client != null) return;

            _options = RabbitMqOptions.Load();
            _client = new RabbitMqClient(_options);

            _client.EngineRegisteredReceived += (s, msg) =>
            {
                AppendStatus($"EngineRegistered: EngineId={msg.JobEntity?.EngineId}");
            };

            _client.ExecuteStoryboardRequestReceived += (s, msg) =>
            {
                AppendStatus($"ExecuteStoryboardRequest: UUId={msg.JobEntity?.UUId}, StoryboardId={msg.JobEntity?.StoryboardId}");
            };

            _client.QueryExecutionStatusRequestReceived += (s, msg) =>
            {
                AppendStatus($"QueryExecutionStatusRequest: UUId={msg.JobEntity?.UUId}, TaskId={msg.JobEntity?.TaskId}");
            };

            _client.UnknownCommandReceived += (s, json) =>
            {
                AppendStatus($"Unknown command: {json}");
            };
        }

        private void EnsureConnectedAndRegister()
        {
            try
            {
                EnsureClient();
                if (!_connected)
                {
                    _client.Start();
                    _connected = true;
                    AppendStatus($"Connected to RabbitMQ {_options.HostName}:{_options.Port}, Down={_options.DownQueueName}, Up={_options.UpQueueName}");
                }

                _client.SendRegister(_options.UpQueueName);
                AppendStatus("Register message sent.");
            }
            catch (Exception ex)
            {
                AppendStatus("Error: " + ex.Message);
            }
        }

        private void SendHeartbeat()
        {
            if (!_connected)
            {
                AppendStatus("Not connected. Please click 'Connect && Register' first.");
                return;
            }

            var engineId = _engineIdBox.Text.Trim();
            if (string.IsNullOrEmpty(engineId))
            {
                AppendStatus("EngineId is empty.");
                return;
            }

            try
            {
                _client.SendHeartbeat(engineId);
                AppendStatus($"Heartbeat sent. EngineId={engineId}");
            }
            catch (Exception ex)
            {
                AppendStatus("Error sending heartbeat: " + ex.Message);
            }
        }

        private void SendTestOk()
        {
            if (!_connected)
            {
                AppendStatus("Not connected. Please click 'Connect && Register' first.");
                return;
            }

            var engineId = _engineIdBox.Text.Trim();
            if (string.IsNullOrEmpty(engineId))
            {
                engineId = "engine-1";
            }

            try
            {
                var resp = new ExecuteResponseEntity
                {
                    UUId = Guid.NewGuid().ToString(),
                    TaskId = "test-task",
                    Status = TaskStatus.DoneWithOk,
                    Message = "Test OK response from MessageAgent UI",
                    Result = null
                };

                _client.SendExecuteStoryboardResponse(resp);
                AppendStatus($"Test OK response sent. EngineId={engineId}, TaskId={resp.TaskId}");
            }
            catch (Exception ex)
            {
                AppendStatus("Error sending test OK: " + ex.Message);
            }
        }

        private void AppendStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendStatus), text);
                return;
            }

            var line = $"[{DateTime.Now:HH:mm:ss}] {text}";
            _statusBox.AppendText(line + Environment.NewLine);
        }

        private void MessageNavigateForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 隐藏而不是销毁，由托盘菜单控制生命周期
            e.Cancel = true;
            Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
                _client = null;
            }
            base.Dispose(disposing);
        }

        private static Icon LoadExeIconFromPng()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var imgPath = Path.Combine(baseDir, "images", "mars_exe.png");
                if (!File.Exists(imgPath))
                    return null;

                using (var bmp = new Bitmap(imgPath))
                {
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

