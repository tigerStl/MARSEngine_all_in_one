using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Mars.MessageCenter.RabbitClient;

namespace MARSMessageAgent
{
    /// <summary>
    /// 在系统任务栏右下角创建并管理托盘图标。
    /// 右键菜单：About..., Message Navigate, Exit。双击托盘图标可打开 Message Navigate。
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Icon _trayIconOwned; // 持有图标引用，避免被 GC 回收导致托盘不显示
        private bool _disposed;
        private Action _onExit;
        private Form _messageNavigateForm;

        public bool IsVisible => _notifyIcon != null && _notifyIcon.Visible;

        /// <summary>
        /// 在任务栏右下角显示托盘图标。
        /// </summary>
        public void Show(string toolTip = "MARS Message Agent", Icon icon = null, Action onExit = null)
        {
            _onExit = onExit;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                if (!string.IsNullOrEmpty(toolTip)) _notifyIcon.Text = toolTip;
                return;
            }

            // 优先使用传入的 icon，否则从文件加载（先 .ico 再 .png），保证托盘一定有有效图标
            Icon trayIcon = icon ?? LoadTrayIcon();
            if (trayIcon == null)
                trayIcon = SystemIcons.Application;

            _trayIconOwned = trayIcon;

            _notifyIcon = new NotifyIcon
            {
                Text = toolTip ?? "MARS Message Agent",
                Icon = _trayIconOwned,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowMessageNavigate();

            var menu = new ContextMenuStrip();

            var aboutItem = new ToolStripMenuItem("About...");
            aboutItem.Click += (s, e) =>
            {
                using (var about = new AboutForm())
                    about.ShowDialog();
            };
            menu.Items.Add(aboutItem);

            var messageNavItem = new ToolStripMenuItem("Message Navigate");
            messageNavItem.Click += (s, e) => ShowMessageNavigate();
            menu.Items.Add(messageNavItem);

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

        private void ShowMessageNavigate()
        {
            if (_messageNavigateForm == null || _messageNavigateForm.IsDisposed)
            {
                _messageNavigateForm = new MessageNavigateForm();
                _messageNavigateForm.StartPosition = FormStartPosition.CenterScreen;
                _messageNavigateForm.Show();
            }
            else
            {
                if (_messageNavigateForm.WindowState == FormWindowState.Minimized)
                    _messageNavigateForm.WindowState = FormWindowState.Normal;
                _messageNavigateForm.Activate();
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
            if (_trayIconOwned != null && _trayIconOwned != SystemIcons.Application)
            {
                _trayIconOwned.Dispose();
                _trayIconOwned = null;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 加载托盘图标：先尝试 .ico（最稳定），再尝试 .png，保证返回可用的 Icon 或 null。
        /// </summary>
        private static Icon LoadTrayIcon()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
                return null;

            // 1. 优先使用 .ico，Windows 托盘对 ICO 支持最好
            var icoPath = Path.Combine(baseDir, "images", "mars_message_center_icon.ico");
            if (File.Exists(icoPath))
            {
                try
                {
                    return new Icon(icoPath, 16, 16);
                }
                catch
                {
                    try
                    {
                        return new Icon(icoPath);
                    }
                    catch { }
                }
            }

            // 2. 备选：mars_tray_icon.png，转为 Icon 并 Clone 成独立副本
            var pngPath = Path.Combine(baseDir, "images", "mars_exe.png");
            if (File.Exists(pngPath))
            {
                try
                {
                    using (var bmp = new Bitmap(pngPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        var icon = Icon.FromHandle(hIcon);
                        Icon clone = (Icon)icon.Clone();
                        NativeMethods.DestroyIcon(hIcon);
                        return clone;
                    }
                }
                catch { }
            }

            return null;
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool DestroyIcon(IntPtr handle);
        }
    }
}
