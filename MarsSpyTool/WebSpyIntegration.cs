using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using Mars.message.Inter.MQCenter.objectSpy;
using MARS.WebAutomation;

namespace MarsSpyTool
{
    /// <summary>
    /// Bridges Finder-tool web drops to MARS.WebAutomation (see doc/需求.md).
    /// </summary>
    internal static class WebSpyIntegration
    {
        public static bool IsLikelyWebBrowser(Process p, IntPtr hwnd)
        {
            if (p == null)
                return false;

            var name = p.ProcessName ?? string.Empty;
            var keys = new[]
            {
                "chrome", "msedge", "firefox", "iexplore", "brave", "opera", "vivaldi", "webkit"
            };
            foreach (var k in keys)
            {
                if (name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            var cls = new StringBuilder(512);
            if (GetClassName(hwnd, cls, cls.Capacity) > 0)
            {
                var c = cls.ToString();
                if (c.IndexOf("Chrome_Widget", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (c.Equals("MozillaWindowClass", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// When the Finder drops on a web browser, opens MARS.WebAutomation and returns true so the legacy spy path is skipped.
        /// </summary>
        public static bool TryHandleFinderWebDrop(Process p, IntPtr hwnd, int x, int y)
        {
            if (p == null)
                return false;
            if (!IsLikelyWebBrowser(p, hwnd))
                return false;

            if (!WebAutomationApp.HasAnyCdpDebugPort())
            {
                MessageBox.Show(
                    GetText("WebSpy.CdpFallback.Message"),
                    GetText("WebSpy.CdpFallback.Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Fallback to SpyTool native UIA positioning flow.
                MarsObjSpyForm.StartAccessibleModeFromXY(p.ProcessName, p.Id, hwnd, x, y);
                return true;
            }

            var uiName = string.Empty;
            var aid = string.Empty;
            var cls = string.Empty;
            try
            {
                var el = AutomationElement.FromPoint(new System.Windows.Point(x, y));
                if (el != null)
                {
                    uiName = el.Current.Name ?? string.Empty;
                    aid = el.Current.AutomationId ?? string.Empty;
                    cls = el.Current.ClassName ?? string.Empty;
                }
            }
            catch
            {
                // UIA may fail for some surfaces; still open WebAutomation with empty hints.
            }

            var ctx = new WebSpyPickContext
            {
                TargetHwnd = hwnd,
                ScreenX = x,
                ScreenY = y,
                ProcessName = p.ProcessName,
                AutomationId = aid,
                UiName = uiName,
                ClassName = cls
            };

            WebAutomationApp.ShowWorkbenchForWebSpy(ctx);
            return true;
        }

        private static string GetText(string key)
        {
            var lang = (ConfigurationManager.AppSettings["UiLanguage"] ?? "en").Trim();
            var isZh = lang.IndexOf("zh", StringComparison.OrdinalIgnoreCase) == 0;

            if (isZh)
            {
                switch (key)
                {
                    case "WebSpy.CdpFallback.Title":
                        return "Web 调试端口提示";
                    case "WebSpy.CdpFallback.Message":
                        return "未检测到调试端口，将转为 SpyTool 的 UIA 定位模式。";
                }
            }

            switch (key)
            {
                case "WebSpy.CdpFallback.Title":
                    return "Web Debug Port";
                case "WebSpy.CdpFallback.Message":
                    return "Debug port was not detected. Switching to SpyTool UIA locating mode.";
                default:
                    return key;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    }
}
