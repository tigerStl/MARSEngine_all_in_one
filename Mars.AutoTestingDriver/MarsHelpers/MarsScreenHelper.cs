using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    }
}
