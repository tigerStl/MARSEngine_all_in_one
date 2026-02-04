using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MARSMessageAgent.Packets;

namespace MARSMessageAgent
{
    /// <summary>
    /// 共享的 Agent 服务：托盘 + WebSocket + HTTP 端口发现。
    /// 供 COM 激活与 exe 独立运行时共用；非 IE 模式通过 HTTP 获取 WebSocket 端口。
    /// </summary>
    public static class AgentServer
    {
        private const int DiscoveryPort = 10005;
        private static TrayIconManager _tray;
        private static WebSocketServerManager _wsServer;
        private static HttpListener _httpListener;
        private static Thread _httpThread;
        private static readonly object _lock = new object();
        private static string _sessionId;

        public static int WebSocketPort => _wsServer?.BoundPort ?? 0;
        public static bool IsRunning => _wsServer != null && _wsServer.IsRunning;

        /// <summary>
        /// 启动托盘、WebSocket 服务与 HTTP 端口发现（10005）。可重复调用，已启动则仅更新 SessionId。
        /// </summary>
        public static void Start(string sessionId = null)
        {
            lock (_lock)
            {
                if (sessionId != null) _sessionId = sessionId;
                if (_sessionId == null) _sessionId = Guid.NewGuid().ToString();

                if (_wsServer == null || !_wsServer.IsRunning)
                {
                    _wsServer = new WebSocketServerManager();
                    if (!_wsServer.Start())
                        throw new InvalidOperationException("Could not start WebSocket server: no free port from 10010.");
                }

                if (_tray == null)
                {
                    _tray = new TrayIconManager();
                    var port = _wsServer.BoundPort;
                    _tray.Show($"MARS Message Agent (WS: {port})", null, onExit: () =>
                    {
                        Shutdown();
                        System.Windows.Forms.Application.Exit();
                    });
                }
                else
                {
                    _tray.SetToolTip($"MARS Message Agent (WS: {_wsServer.BoundPort})");
                }

                StartDiscovery();
            }
        }

        private static void StartDiscovery()
        {
            if (_httpListener != null) return;
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://127.0.0.1:" + DiscoveryPort + "/");
                _httpListener.Start();
                _httpThread = new Thread(DiscoveryLoop) { IsBackground = true };
                _httpThread.Start();
            }
            catch { }
        }

        private static void DiscoveryLoop()
        {
            while (_httpListener != null)
            {
                try
                {
                    var ctx = _httpListener.GetContext();
                    var port = WebSocketPort;
                    var json = "{\"port\":" + port + "}";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    ctx.Response.ContentType = "application/json; charset=utf-8";
                    ctx.Response.ContentLength64 = bytes.Length;
                    ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    ctx.Response.Close();
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
                catch { }
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                try { _httpListener?.Stop(); _httpListener?.Close(); } catch { }
                _httpListener = null;
                _wsServer?.Stop();
                _wsServer = null;
                _tray?.Hide();
                _tray?.Dispose();
                _tray = null;
            }
        }

        /// <summary>
        /// HTTP 端口发现服务地址，供非 IE 页面 fetch 获取 WebSocket 端口。
        /// </summary>
        public static string DiscoveryUrl => "http://127.0.0.1:" + DiscoveryPort + "/";

        /// <summary>
        /// Sends a shakeHand packet (with wsServerPort) to the Driver listening on marsWebSocketServerPort.
        /// Call after Start() so WebSocketPort is set. Indicates communication is established.
        /// </summary>
        public static bool SendHandshakeToDriver(string sessionId, int marsWebSocketServerPort)
        {
            int wsPort = WebSocketPort;
            if (wsPort <= 0 || marsWebSocketServerPort <= 0) return false;
            try
            {
                var packet = new ShakeHandResponse(sessionId, "OK", wsPort);
                string json = PacketFactory.ToJson(packet) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, marsWebSocketServerPort);
                    using (var stream = client.GetStream())
                        stream.Write(bytes, 0, bytes.Length);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
