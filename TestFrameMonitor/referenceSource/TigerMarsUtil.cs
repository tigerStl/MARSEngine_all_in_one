using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace MarsTestFrame.systemUtil
{
    public class MouseSimulator
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MouseSimulator));
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }
        [StructLayout(LayoutKind.Explicit)]
        struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)]
            public MouseInputData mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }
        struct MouseInputData
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [Flags]
        enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }
        enum SendInputEventType : int
        {
            InputMouse = 0,
            InputKeyboard = 1,
            InputHardware = 2
        }



        public static void ClickLeftMouseButton()
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            INPUT mouseUpInput = new INPUT();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }
        public static void ClickRightMouseButton()
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            INPUT mouseUpInput = new INPUT();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }

        public static void ScrollMouseTo(int iNum = 10000)
        {
            Logger.logBegin("ScrollMouseTo");
            INPUT input = new INPUT();
            input.type = SendInputEventType.InputMouse;
            //input.mkhi.mi = new MouseInputData();
            input.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_WHEEL;
            input.mkhi.mi.dwExtraInfo = IntPtr.Zero;
            input.mkhi.mi.dx = 0;
            input.mkhi.mi.dy = 0;
            input.mkhi.mi.time = 0;
            input.mkhi.mi.mouseData = (uint)(iNum * 120);
            uint iError = SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            Logger.Info("ScrollMouseTo", string.Format("size:[{0}],iError=[{1}]", Marshal.SizeOf(typeof(INPUT)), iError));

            //Thread.Sleep(1000);
            //mouse_event((int)MouseEventFlags.MOUSEEVENTF_WHEEL, 0, 0, 200, 0);
            //Logger.Info("ScrollMouseTo", "Mouse_event");
        }


    }

    internal sealed class AddinsFuncs4VBS
    {
        internal static bool Increase_lastN(string strSrc, int iLstChngNum, int iCreaseBy, ref string strResult, ref string strError)
        {
            if (string.IsNullOrEmpty(strSrc))
            {
                strError = "input string is null or empty!";
                return false;
            }
            if (strSrc.Length < iLstChngNum)
            {
                strError = string.Format("input string [{0}] is less than [{1}]", strSrc, iLstChngNum);
                return false;
            }
            string strLstN = strSrc.Substring(strSrc.Length - iLstChngNum);
            int iN = 0;
            if (!int.TryParse(strLstN, out iN))
            {
                strError = string.Format("Last [{0}] characters are not number,---[{1}]", iLstChngNum, strLstN);
                return false;
            }
            string strTmpNumber = (((int)Math.Pow(10, iLstChngNum)) + (iN + 1) + "").Substring(1);
            strResult = string.Format("{0}{1}", strSrc.Substring(0, strSrc.Length - iLstChngNum), strTmpNumber);

            return true;
        }

    }

    public class TigerMarsUtil
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(TigerMarsUtil));
        public static string GetPathWithoutFileName(string strFileWithPath)
        {
            Logger.logBegin("GetPathWithoutFileName");
            try
            {
                if (strFileWithPath == null) return null;

                int iLastPos = strFileWithPath.LastIndexOf("\\");
                if (iLastPos == -1)
                {
                    return null;
                }

                return strFileWithPath.Substring(0, iLastPos);

            }
            finally
            {
                Logger.logEnd("GetPathWithoutFileName");

            }
        }

        public static string GetParameter(string strParaName, string strValue)
        {
            return string.Format(" ,[{0}={1}] ", strParaName, strValue);
        }

        public static string GetParameter(string[] arrParaName, string[] strValues)
        {
            string strFormat = "";
            int iMaxLen = arrParaName == null ? -1 : arrParaName.Length;
            iMaxLen = Math.Max(iMaxLen, strValues == null ? -1 : strValues.Length);
            for (int i = 0; i < iMaxLen; i++)
            {
                strFormat = string.Format("{0},[{1}={2}]", strFormat, arrParaName[i], strValues[i]);
            }
            return strFormat;
        }

        public static bool RegularTest(string strPartern, string strValue)
        {
            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            //MatchCollection lst = Regex.Matches(strValue, strPartern);

            return Regex.IsMatch(strValue, strPartern, options);
        }

        #region Mouse


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        public static void LeftMouseClick(int xpos, int ypos)
        {
            MouseSimulator.SetCursorPos(xpos, ypos);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 1, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 1, 0);
        }

        public void LeftMouseClickByInstance(int xpos, int ypos)
        {
            MouseSimulator.SetCursorPos(xpos, ypos);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 1, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 1, 0);
        }
        #endregion //Mouse
    }
}
