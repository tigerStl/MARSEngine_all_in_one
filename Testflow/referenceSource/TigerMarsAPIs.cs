using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TestFlowClient.referenceSource
{
    public class TigerMarsAPIs
    {
        #region message
        public const int WM_IME_NOTIFY = 0x0282;
        public const int WM_DESTROY = 0x0002;
        public const int WM_NCDESTROY = 0x0082;
        public const int WM_CLOSE = 0x0010;
        public const int IMN_CLOSESTATUSWINDOW = 0x0001;
        public const int WM_KILLFOCUS = 0x0008;
        public const int WM_COMMAND = 0x0011;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hwnd, int msg, int wParam, StringBuilder lParam);
        [DllImport("user32.dll")]
        public static extern int PostMessage(IntPtr hwnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        #endregion //Message

        #region Mouse
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 1, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 1, 0);
        }
        #endregion //Mouse

    }
}
