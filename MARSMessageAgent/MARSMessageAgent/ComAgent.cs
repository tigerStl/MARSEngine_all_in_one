using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MARSMessageAgent
{
    /// <summary>
    /// COM 可见的 Agent 类。可通过 CreateObject 激活，可选传入 SessionId（GUID）。
    /// 激活后在任务栏右下角创建托盘图标，并启动从 10010 开始的 WebSocket 服务。
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("C1D2E3F4-A5B6-4C78-9DEF-0123456789AB")]
    [ProgId("MARSMessageAgent.Agent")]
    public class ComAgent
    {
        private string _sessionId;

        /// <summary>
        /// 当前 SessionId（可由 JS 在创建后通过属性或方法传入）。
        /// </summary>
        public string SessionId
        {
            get => _sessionId ?? string.Empty;
            set => _sessionId = value;
        }

        /// <summary>
        /// 当前 WebSocket 服务绑定端口，未启动时为 0。
        /// </summary>
        public int WebSocketPort => AgentServer.WebSocketPort;

        /// <summary>
        /// 激活 Agent：显示托盘图标并启动 WebSocket 服务。
        /// 可从 JS 调用：var agent = new ActiveXObject("MARSMessageAgent.Agent"); agent.Activate("guid-session-id");
        /// </summary>
        /// <param name="sessionId">可选。用于标识会话的 GUID</param>
        public void Activate(string sessionId = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                sessionId = Guid.NewGuid().ToString();
            _sessionId = sessionId;
            AgentServer.Start(sessionId);
        }

        /// <summary>
        /// 仅启动（不传参时也可先设置 SessionId 再调用 Activate()）。
        /// </summary>
        public void Activate()
        {
            AgentServer.Start(_sessionId);
        }

        /// <summary>
        /// 关闭 WebSocket 服务并隐藏托盘图标。
        /// </summary>
        public void Shutdown()
        {
            AgentServer.Shutdown();
        }
    }
}
