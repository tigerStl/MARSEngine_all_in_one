using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MARSMessageAgent.MessageCenter.MarsWebSocketServer
{
    /// <summary>
    /// 本地 WebSocket 服务（用于本地引擎状态与握手）。
    /// - 应用启动时一起启动；
    /// - 从 10000 开始选择空闲端口；
    /// - 将端口写入应用目录下 MarsLocalSvcSwap\wsSwapLoal-[windowsAccountName].json；
    /// - 接收本地引擎的 handshake 与 status 报文，并回调状态完成事件。
    /// </summary>
    public sealed class MarsLocalWebSocketServer
    {
        private const int StartPort = 10000;
        private const int MaxPortTry = 100;

        private static readonly Lazy<MarsLocalWebSocketServer> _instance =
            new Lazy<MarsLocalWebSocketServer>(() => new MarsLocalWebSocketServer());

        public static MarsLocalWebSocketServer Instance => _instance.Value;

        private WebSocketServer _server;
        private readonly List<IWebSocketConnection> _sockets = new List<IWebSocketConnection>();
        private readonly object _lock = new object();

        public int BoundPort { get; private set; }
        public bool IsRunning => _server != null;

        /// <summary>
        /// 当 TestCase 或 Storyboard 结束（Status == DONE/OK/FAILED）时回调，
        /// 供上层通过 RabbitMQ 上报到 WebServer。
        /// </summary>
        public static event Action<EngineStatusReportRequest> EngineStatusCompleted;

        private MarsLocalWebSocketServer()
        {
        }

        public static void Start()
        {
            Instance.StartInternal();
        }

        private void StartInternal()
        {
            if (_server != null)
                return;

            var port = FindAvailablePort(StartPort, MaxPortTry);
            if (port < 0)
                return;

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

                WriteSwapFile(port);
            }
            catch
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

        private static void WriteSwapFile(int port)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var swapDir = Path.Combine(baseDir, "MarsLocalSvcSwap");
                if (!Directory.Exists(swapDir))
                {
                    Directory.CreateDirectory(swapDir);
                }

                var userName = Environment.UserName ?? "UnknownUser";
                var fileName = $"wsSwapLoal-{userName}.json";
                var filePath = Path.Combine(swapDir, fileName);

                var payload = new
                {
                    DateTime = DateTime.Now,
                    port = port
                };

                var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // 写 swap 文件失败不影响服务本身
            }
        }

        private void OnMessage(IWebSocketConnection socket, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                var jObj = JObject.Parse(message);

                // 通过字段判断消息类型
                if (jObj["TestRunningStatus"] != null || jObj["TestStoryBoardInfo"] != null || jObj["TestCaseInfo"] != null)
                {
                    HandleEngineStatusReport(socket, jObj);
                }
                else
                {
                    HandleShakeHand(socket, jObj);
                }
            }
            catch (Exception ex)
            {
                LocalLog("OnMessage", "Parse error: " + ex.Message, ex);
            }
        }

        private static void HandleShakeHand(IWebSocketConnection socket, JObject jObj)
        {
            var req = jObj.ToObject<ShakeHandRequestLocal>() ?? new ShakeHandRequestLocal();

            var resp = new ShakeHandResponseLocal
            {
                DateTime = DateTime.Now,
                version = req.version,
                UUID = req.UUID
            };

            var json = JsonConvert.SerializeObject(resp);
            socket.Send(json);
        }

        private static readonly string[] CompletedStatuses = { "DONE", "OK", "FAILED" };

        private static void HandleEngineStatusReport(IWebSocketConnection socket, JObject jObj)
        {
            var report = jObj.ToObject<EngineStatusReportRequest>() ?? new EngineStatusReportRequest();

            // 保存最新一条
            MARSLocalEngineStatusReport.SetLatest(report);

            // 记录到 log
            LocalLog("EngineStatusReport", JsonConvert.SerializeObject(report));

            // 响应
            var resp = new EngineStatusReportResponse
            {
                DateTime = DateTime.Now,
                version = report.version,
                UUID = report.UUID
            };
            socket.Send(JsonConvert.SerializeObject(resp));

            // 当用例或 Storyboard 结束时回调
            bool isCaseCompleted = report.TestCaseInfo != null &&
                                   !string.IsNullOrEmpty(report.TestCaseInfo.Status) &&
                                   CompletedStatuses.Contains(report.TestCaseInfo.Status, StringComparer.OrdinalIgnoreCase);

            bool isStoryboardCompleted = report.TestStoryBoardInfo != null &&
                                         !string.IsNullOrEmpty(report.TestStoryBoardInfo.Status) &&
                                         CompletedStatuses.Contains(report.TestStoryBoardInfo.Status, StringComparer.OrdinalIgnoreCase);

            if (isCaseCompleted || isStoryboardCompleted)
            {
                try
                {
                    EngineStatusCompleted?.Invoke(report);
                }
                catch (Exception ex)
                {
                    LocalLog("EngineStatusCompletedCallback", "Callback error: " + ex.Message, ex);
                }
            }
        }

        private static readonly object _logLock = new object();
        private static string _logPath;

        private static void LocalLog(string method, string message, Exception ex = null)
        {
            try
            {
                lock (_logLock)
                {
                    if (_logPath == null)
                    {
                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        var logDir = Path.Combine(baseDir, "log");
                        if (!Directory.Exists(logDir))
                        {
                            Directory.CreateDirectory(logDir);
                        }
                        _logPath = Path.Combine(logDir, "MessageCenterLog.log");
                    }

                    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [LOCALWS] {method} {message}";
                    if (ex != null)
                    {
                        line += Environment.NewLine + ex;
                    }
                    File.AppendAllText(_logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // 忽略日志异常
            }
        }
    }

    #region DTOs & 状态缓存

    public sealed class ShakeHandRequestLocal
    {
        public DateTime DateTime { get; set; }
        public string version { get; set; }
        public string UUID { get; set; }
    }

    public sealed class ShakeHandResponseLocal
    {
        public DateTime DateTime { get; set; }
        public string version { get; set; }
        public string UUID { get; set; }
    }

    public sealed class EngineStatusReportRequest
    {
        public DateTime DateTime { get; set; }
        public string version { get; set; }
        public string UUID { get; set; }

        public string TestRunningStatus { get; set; }
        public string statusMessage { get; set; }

        public TestStoryboardInfo TestStoryBoardInfo { get; set; }
        public TestCaseInfo TestCaseInfo { get; set; }
        public TestStepInfo TestStepInfo { get; set; }
    }

    public sealed class EngineStatusReportResponse
    {
        public DateTime DateTime { get; set; }
        public string version { get; set; }
        public string UUID { get; set; }
    }

    public sealed class TestStoryboardInfo
    {
        public string TestStoryboardName { get; set; }
        public string TestStoryboardId { get; set; }
        public int TestStoryboardRunOrder { get; set; }
        public string Status { get; set; }
    }

    public sealed class TestCaseInfo
    {
        public string TestCaseName { get; set; }
        public string TestCaseId { get; set; }
        public int TestCaseRunOrder { get; set; }
        public string Status { get; set; }
    }

    public sealed class TestStepInfo
    {
        public string TestKeyword { get; set; }
        public string TestObject { get; set; }
        public string TestPara { get; set; }
        public string TestDataInput { get; set; }
        public string TestDataReturn { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// 保存最新一条引擎状态。
    /// </summary>
    public static class MARSLocalEngineStatusReport
    {
        private static readonly object _lock = new object();
        private static EngineStatusReportRequest _latest;

        internal static void SetLatest(EngineStatusReportRequest report)
        {
            lock (_lock)
            {
                _latest = report;
            }
        }

        public static EngineStatusReportRequest GetLatestEngineStatus()
        {
            lock (_lock)
            {
                return _latest;
            }
        }
    }

    #endregion
}

