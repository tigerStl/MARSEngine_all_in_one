//using OpenQA.Selenium;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.MarsMessageCenter
{
    /// <summary>
    /// Starts MARSMessageAgent.exe with startMessageAgent parameters and optionally
    /// listens on marsWebSocketServerPort to receive the agent's shakeHand (communication established).
    /// </summary>
    public class MARSMessageCenterAgentStarter
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MARSMessageCenterAgentStarter));

        public const string CmdStartMessageAgent = "startMessageAgent";
        private const string AgentExeName = "MARSMessageAgent.exe";
        private const string ReinstallMessage = "MARSMessageAgent.exe was not found. Please reinstall the latest version of MaRS engine.";

        private readonly string _sessionId;
        private readonly int _marsWebSocketServerPort;

        /// <summary>
        /// After Run(), the port on which the agent's WebSocket server is listening (from shakeHand).
        /// </summary>
        public int AgentWebSocketPort { get; private set; }

        /// <summary>
        /// True if the handshake from the agent was received successfully.
        /// </summary>
        public bool HandshakeReceived { get; private set; }

        /// <summary>
        /// MQ local swap file based communicator client.
        /// </summary>
        public MARSWebMessageCenterCommunitorClientStub MQStubCommunicatorClient { get; private set; }

        public MARSMessageCenterAgentStarter(string sessionId, int marsWebSocketServerPort)
        {
            _sessionId = sessionId ?? string.Empty;
            _marsWebSocketServerPort = marsWebSocketServerPort;
        }
        public MARSMessageCenterAgentStarter()
        {
            _sessionId = Guid.NewGuid().ToString();
            _marsWebSocketServerPort = -1;
        }

        /// <summary>
        /// Starts MARSMessageAgent.exe. Returns true if the agent process was started; false if exe not found (dialog shown).
        /// Call WaitForHandshake() (e.g. in a background thread) before this so the Driver is listening when the Agent sends the handshake.
        /// </summary>
        public bool StartAgent(out string errorMessage)
        {
            errorMessage = null;
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string agentPath = Path.Combine(appDir, AgentExeName);

            if (!File.Exists(agentPath))
            {
                MessageBox.Show(ReinstallMessage, "MaRS Engine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                errorMessage = ReinstallMessage;
                return false;
            }

            string arguments = BuildArguments();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = agentPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    WorkingDirectory = appDir
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                MessageBox.Show($"Failed to start MARS Message Agent: {ex.Message}", "MaRS Engine", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Connects to local MQ stub by process check + local swap file discovery.
        /// 1) Ensure MarsMessageAgent.exe (current session/user) is running, otherwise start and wait 10s.
        /// 2) Read MarsLocalSvcSwap\wsSwapLoal-[windowsAccountName].json from assembly dir.
        /// 3) If missing/invalid, return false.
        /// 4) Create MARSWebMessageCenterCommunitorClientStub and store info.
        /// 5) Start heartbeat thread to send ShakeHandRequestLocal every minute.
        /// </summary>
        public bool ConnectToMQStub(out string errorMessage)
        {
            logger.logBegin("ConnectToMQStub");
            errorMessage = null;
            try
            {
                if (!IsAgentRunningInCurrentSession())
                {
                    if (!StartAgent(out errorMessage))
                        return false;
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string windowsAccountName = Environment.UserName;
                string swapDir = Path.Combine(appDir, "MarsLocalSvcSwap");
                string swapFilePath = Path.Combine(swapDir, $"wsSwapLoal-{windowsAccountName}.json");
                if (!File.Exists(swapFilePath))
                {
                    errorMessage = $"Cannot find local MQ swap file: {swapFilePath}. Possible RabbitMQ communication issue or old agent version.";
                    return false;
                }

                string json;
                try
                {
                    json = File.ReadAllText(swapFilePath, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    logger.Error("ConnectToMQStub", ex.Message, ex);
                    errorMessage = $"Failed to read swap file: {ex.Message}";
                    return false;
                }

                if (!TryParseSwapPort(json, out int port))
                {
                    errorMessage = $"Invalid swap file format or missing port in: {swapFilePath}";
                    return false;
                }

                MQStubCommunicatorClient?.Stop();
                MQStubCommunicatorClient = new MARSWebMessageCenterCommunitorClientStub(
                    windowsAccountName,
                    port,
                    DateTime.Now,
                    json,
                    _sessionId);
                MQStubCommunicatorClient.Start();
                return true;
            }
            finally
            {
                logger.logEnd("ConnectToMQStub", $"{errorMessage}" );
            }
        }

        /// <summary>
        /// Builds command-line arguments: -cmd startMessageAgent -sessionId "..." -marsWebSocketServerPort N
        /// </summary>
        public string BuildArguments()
        {
            string sessionArg = string.IsNullOrEmpty(_sessionId)
                ? "-sessionId \"\""
                : $"-sessionId \"{_sessionId}\"";
            return $"-cmd {CmdStartMessageAgent} {sessionArg} -marsWebSocketServerPort {_marsWebSocketServerPort}";
        }

        /// <summary>
        /// Starts a one-shot TCP listener on marsWebSocketServerPort, waits for one connection and one JSON line (shakeHand),
        /// parses agentWsPort from the payload and sets HandshakeReceived / AgentWebSocketPort.
        /// Call this after StartAgent() if you need to know when the agent has sent the handshake.
        /// </summary>
        public void WaitForHandshake(int timeoutMs = 30000)
        {
            HandshakeReceived = false;
            AgentWebSocketPort = 0;
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, _marsWebSocketServerPort);
                listener.Start();
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeoutMs);
                    var clientTask = listener.AcceptTcpClientAsync();
                    if (!clientTask.Wait(timeoutMs, cts.Token))
                        return;
                    using (var client = clientTask.Result)
                    using (var stream = client.GetStream())
                    {
                        var buffer = new byte[4096];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0) return;
                        string json = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                        int port = ParseAgentWsPortFromShakeHand(json);
                        if (port > 0)
                        {
                            AgentWebSocketPort = port;
                            HandshakeReceived = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Timeout or parse error; leave HandshakeReceived false
            }
            finally
            {
                try { listener?.Stop(); } catch { }
            }
        }

        /// <summary>
        /// Parses JSON shakeHand (ShakeHandle_response) and returns wsServerPort if present.
        /// </summary>
        private static int ParseAgentWsPortFromShakeHand(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var portToken = obj["wsServerPort"] ?? obj["marsAgentWsPort"];
                if (portToken != null && int.TryParse(portToken.ToString(), out int port) && port > 0)
                    return port;
            }
            catch { }
            return 0;
        }

        private static bool TryParseSwapPort(string content, out int port)
        {
            port = 0;
            if (string.IsNullOrWhiteSpace(content)) return false;

            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(content);
                var portToken = obj["port"] ?? obj["wsServerPort"] ?? obj["marsAgentWsPort"];
                if (portToken != null && int.TryParse(portToken.ToString(), out port) && port > 0)
                    return true;
            }
            catch
            {
                // fall through to tolerate non-standard pseudo-json format
            }

            var match = Regex.Match(content, @"port\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out port) && port > 0)
                return true;

            return false;
        }

        private static bool IsAgentRunningInCurrentSession()
        {
            string processName = Path.GetFileNameWithoutExtension(AgentExeName);
            int currentSessionId = Process.GetCurrentProcess().SessionId;
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (proc.SessionId == currentSessionId)
                        return true;
                }
                catch
                {
                    // ignore inaccessible process and continue
                }
                finally
                {
                    proc.Dispose();
                }
            }
            return false;
        }
    }

    public class MARSWebMessageCenterCommunitorClientStub
    {
        private const string ShakeHandRequestLocal = "ShakeHandRequestLocal";

        private readonly string _sessionId;
        private readonly object _sync = new object();
        private Thread _heartbeatThread;
        private bool _running;

        public string WindowsAccountName { get; }
        public int Port { get; }
        public DateTime DateTime { get; }
        public string SwapRawContent { get; }

        public MARSWebMessageCenterCommunitorClientStub(string windowsAccountName, int port, DateTime dateTime, string swapRawContent, string sessionId)
        {
            WindowsAccountName = windowsAccountName ?? string.Empty;
            Port = port;
            DateTime = dateTime;
            SwapRawContent = swapRawContent ?? string.Empty;
            _sessionId = sessionId ?? string.Empty;
        }

        public void Start()
        {
            lock (_sync)
            {
                if (_running) return;
                _running = true;
                _heartbeatThread = new Thread(HeartbeatLoop)
                {
                    IsBackground = true,
                    Name = "MARSWebMessageCenterCommunitorClientStub-Heartbeat"
                };
                _heartbeatThread.Start();
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _running = false;
            }
        }

        private void HeartbeatLoop()
        {
            while (IsRunning())
            {
                TrySendShakeHand();

                // Sleep in small chunks so Stop() can take effect quickly.
                for (int i = 0; i < 60 && IsRunning(); i++)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }

        private bool IsRunning()
        {
            lock (_sync)
            {
                return _running;
            }
        }

        private void TrySendShakeHand()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, Port);
                    using (var stream = client.GetStream())
                    {
                        string payload = BuildShakeHandPayload();
                        byte[] bytes = Encoding.UTF8.GetBytes(payload + "\n");
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();
                    }
                }
            }
            catch
            {
                // Best-effort heartbeat.
            }
        }

        private string BuildShakeHandPayload()
        {
            var obj = new Newtonsoft.Json.Linq.JObject
            {
                ["messageType"] = ShakeHandRequestLocal,
                ["command"] = ShakeHandRequestLocal,
                ["sessionId"] = _sessionId,
                ["windowsAccountName"] = WindowsAccountName,
                ["dateTime"] = System.DateTime.Now.ToString("o")
            };
            return obj.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
