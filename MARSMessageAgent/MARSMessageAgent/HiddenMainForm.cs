using System;
using System.Windows.Forms;

namespace MARSMessageAgent
{
    /// <summary>
    /// 隐藏主窗体，仅用于保持消息循环，供 COM 与托盘使用。
    /// 独立运行 exe 时也会启动 Agent 服务，供非 IE 模式通过 HTTP 发现端口并连接 WebSocket。
    /// </summary>
    public class HiddenMainForm : Form
    {
        public HiddenMainForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Size = new System.Drawing.Size(0, 0);
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(-32000, -32000);
            Shown += (s, e) => { Visible = false; Hide(); };
            Load += HiddenMainForm_Load;
        }

        private void HiddenMainForm_Load(object sender, EventArgs e)
        {
            if (!AgentServer.IsRunning)
                AgentServer.Start();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!IsHandleCreated && value)
                CreateHandle();
            base.SetVisibleCore(false);
        }
    }
}
