using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Mars.AutoTestingDriver.MarsHelpers
{

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }
    public class MarsScreenHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        public static Screen currentProcessHost { get; set; } = null;
        public static string currentProcessName { get; set; } = string.Empty;

        public static Screen? GetProcessMainWindowScreen(string processName)
        {
            var processes = Process.GetProcessesByName(processName);

            foreach (var proc in processes)
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    if (GetWindowRect(proc.MainWindowHandle, out RECT rect))
                    {
                        var windowCenter = new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);

                        foreach (var screen in Screen.AllScreens)
                        {
                            if (screen.Bounds.Contains(windowCenter))
                                return currentProcessHost=screen;
                        }
                    }
                }
            }

            return currentProcessHost=null;
        }

        public static Screen? GetProcessMainWindowScreen()
        {
            if (string.IsNullOrEmpty(currentProcessName))
            {
                return null;
            }
            else
            {
                return GetProcessMainWindowScreen(currentProcessName);
            }
        }

        /// <summary>
        /// 根据进程 ID 截取该进程主窗口的图像，并以 BMP 格式保存到运行目录下的 tmpimg 目录。
        /// 如果目录不存在则自动创建。返回保存的文件完整路径，失败则返回 null。
        /// </summary>
        /// <param name="processId">目标进程 ID</param>
        /// <returns>保存的 BMP 文件路径，失败为 null</returns>
        public static string CaptureProcessGuiToBmp(int processId)
        {
            try
            {
                Rectangle bounds;
                string processNameForFile;

                var proc = Process.GetProcessById(processId);
                if (proc == null)
                {
                    return null;
                }

                // 如果没有主窗口句柄，则截取整个虚拟屏幕
                if (proc.MainWindowHandle == IntPtr.Zero)
                {
                    bounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                }
                else
                {
                    if (!GetWindowRect(proc.MainWindowHandle, out RECT rect))
                    {
                        return null;
                    }

                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    if (width <= 0 || height <= 0)
                    {
                        return null;
                    }

                    bounds = new Rectangle(rect.Left, rect.Top, width, height);
                }

                processNameForFile = proc.ProcessName;

                // 准备保存目录
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string imgDir = Path.Combine(baseDir, "tmpimg");
                if (!Directory.Exists(imgDir))
                {
                    Directory.CreateDirectory(imgDir);
                }

                string fileName = $"{processNameForFile}_{processId}_{DateTime.Now:yyyyMMdd_HHmmssfff}.jpeg";
                string filePath = Path.Combine(imgDir, fileName);

                using (var bmp = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }
                    bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                return filePath;
            }
            catch
            {
                return null;
            }
        }
    }
}
