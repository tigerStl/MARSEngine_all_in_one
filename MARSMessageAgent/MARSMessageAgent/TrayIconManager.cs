using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MARSMessageAgent
{
    /// <summary>
    /// 在系统任务栏右下角创建并管理托盘图标。
    /// 右键菜单：About..., Exit（退出并销毁 COM 实例）, Engine Path（在 Explorer 中打开引擎所在目录）。
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private bool _disposed;
        private Action _onExit;

        public bool IsVisible => _notifyIcon != null && _notifyIcon.Visible;

        /// <summary>
        /// 在任务栏右下角显示托盘图标。
        /// </summary>
        /// <param name="toolTip">悬停提示文字</param>
        /// <param name="icon">图标，为 null 时使用默认应用图标</param>
        /// <param name="onExit">点击 Exit 时调用（通常先 Shutdown COM 实例再 Application.Exit）</param>
        public void Show(string toolTip = "MARS Message Agent", Icon icon = null, Action onExit = null)
        {
            _onExit = onExit;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                if (!string.IsNullOrEmpty(toolTip)) _notifyIcon.Text = toolTip;
                return;
            }

            _notifyIcon = new NotifyIcon
            {
                Text = toolTip ?? "MARS Message Agent",
                Icon = icon ?? SystemIcons.Application,
                Visible = true
            };

            var menu = new ContextMenuStrip();

            var aboutItem = new ToolStripMenuItem("About...");
            aboutItem.Click += (s, e) =>
            {
                using (var about = new AboutForm())
                    about.ShowDialog();
            };
            menu.Items.Add(aboutItem);

            var enginePathItem = new ToolStripMenuItem("Engine Path");
            enginePathItem.Click += (s, e) => OpenEnginePathInExplorer();
            menu.Items.Add(enginePathItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                _onExit?.Invoke();
                Application.Exit();
            };
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        private static void OpenEnginePathInExplorer()
        {
            var dir = MarsEngineLauncher.GetMarsEngineInstallDirectory();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                MessageBox.Show("Engine install directory not found.", "Engine Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Explorer: " + ex.Message, "Engine Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void Hide()
        {
            if (_notifyIcon != null)
                _notifyIcon.Visible = false;
        }

        public void SetToolTip(string text)
        {
            if (_notifyIcon != null)
                _notifyIcon.Text = text;
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
