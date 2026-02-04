using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Fleck;
using MARSMessageAgent.Packets;

namespace MARSMessageAgent
{
    /// <summary>
    /// 从 10010 开始寻找空闲端口并绑定 WebSocket 服务。
    /// 处理握手与启动 MARS Engine 等消息包。
    /// </summary>
    public class WebSocketServerManager
    {
        private const int StartPort = 10010;
        private const int MaxPortTry = 100;

        private WebSocketServer _server;
        private readonly List<IWebSocketConnection> _sockets = new List<IWebSocketConnection>();
        private readonly object _lock = new object();

        public int BoundPort { get; private set; }
        public bool IsRunning => _server != null;

        /// <summary>
        /// 从 10010 起寻找第一个空闲端口并启动 WebSocket 服务。
        /// </summary>
        public bool Start()
        {
            if (_server != null)
                return true;

            var port = FindAvailablePort(StartPort, MaxPortTry);
            if (port < 0)
                return false;

            try
            {
                _server = new WebSocketServer($"ws://127.0.0.1:{port}");
                _server.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        lock (_lock) { _sockets.Add(socket); }
                    };
                    socket.OnClose = () =>
                    {
                        lock (_lock) { _sockets.Remove(socket); }
                    };
                    socket.OnMessage = message => OnMessage(socket, message);
                    socket.OnError = ex =>
                    {
                        lock (_lock) { _sockets.Remove(socket); }
                    };
                });
                BoundPort = port;
                return true;
            }
            catch
            {
                _server = null;
                return false;
            }
        }

        public void Stop()
        {
            if (_server == null) return;
            try
            {
                foreach (var s in _sockets.ToList())
                {
                    try { s.Close(); } catch { }
                }
                _sockets.Clear();
                _server.Dispose();
            }
            catch { }
            finally
            {
                _server = null;
            }
        }

        private static int FindAvailablePort(int start, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var port = start + i;
                if (IsPortAvailable(port))
                    return port;
            }
            return -1;
        }

        private static bool IsPortAvailable(int port)
        {
            TcpListener tcp = null;
            try
            {
                tcp = new TcpListener(IPAddress.Loopback, port);
                tcp.Start();
                tcp.Stop();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try { tcp?.Stop(); } catch { }
            }
        }

        private void OnMessage(IWebSocketConnection socket, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                var packet = PacketFactory.FromJson(message);

                if (packet is ShakeHandRequest shakeReq)
                {
                    var response = new ShakeHandResponse(shakeReq.SessionId, "OK");
                    socket.Send(PacketFactory.ToJson(response));
                    return;
                }

                if (packet is StartMARSEngineRequest startReq)
                {
                    var (success, msg) = MarsEngineLauncher.TryStart();
                    var result = success ? (object)"Success" : (object)"FAILED";
                    var response = new StartMARSEngineResponse(startReq.SessionId, success, msg);
                    response.Result = result;
                    socket.Send(PacketFactory.ToJson(response));
                    return;
                }

                // 未知请求类型可记录或返回错误
            }
            catch (Exception ex)
            {
                try
                {
                    var sessionId = TryGetSessionId(message);
                    var err = new ShakeHandResponse(sessionId, $"Error: {ex.Message}");
                    socket.Send(PacketFactory.ToJson(err));
                }
                catch { }
            }
        }

        private static string TryGetSessionId(string json)
        {
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                return obj["sessionId"]?.ToString() ?? obj["SessionId"]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
