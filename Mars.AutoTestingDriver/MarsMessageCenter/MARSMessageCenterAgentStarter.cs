using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

        public MARSMessageCenterAgentStarter(string sessionId, int marsWebSocketServerPort)
        {
            _sessionId = sessionId ?? string.Empty;
            _marsWebSocketServerPort = marsWebSocketServerPort;
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
    }
}
