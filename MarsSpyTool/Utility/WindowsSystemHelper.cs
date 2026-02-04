using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyAgent.SpyGui.Utility
{
    public enum MarsWindowsStatus
    {
        en_visible_check=0x01,
        en_hidden_left,
        en_hidden_left_covered,
        en_hidden_right,
        en_hidden_right_covered,
        en_hidden_top,
        en_hidden_top_covered,
        en_hidden_bottom,
        en_hidden_bottom_covered,
        en_allVisible
    }

    internal class WindowsSystemHelper
    {
        public bool isWindowVisible(IntPtr hWnd,ref string strError,ref MarsWindowsStatus currentWindowStatus)
        {
            ///steps:
            ///1, get windows rect
            ///2, check four point with from windows from point
            ///
            bool isOk = MarsWindowsAPIs.IsWindowVisible(hWnd);
            if (!isOk)
            {
                strError = "IsWindowVisible return false";
                currentWindowStatus = MarsWindowsStatus.en_visible_check;
                return false;
            }

            MarsWindowsAPIs.RECT lpRect = new MarsWindowsAPIs.RECT();
            uint iLastError = 0xFFFFFFF;
            isOk=MarsWindowsAPIs.GetWindowRect(hWnd, out lpRect);
            if (!isOk)
            {
                iLastError = MarsWindowsAPIs.GetLastError();
                strError = $"GetWindowRect return {iLastError}";
                currentWindowStatus = MarsWindowsStatus.en_hidden_left;
                return false;
            }
            
            System.Drawing.Point leftTop    = new System.Drawing.Point() { X = lpRect.Left + 3, Y = lpRect.Top + 3 };
            System.Drawing.Point rightTop   = new System.Drawing.Point() { X = lpRect.Right - 3, Y = lpRect.Top + 3 };
            System.Drawing.Point leftBottom = new System.Drawing.Point() { X = lpRect.Left + 3, Y = lpRect.Bottom -3 };
            System.Drawing.Point rightBottom= new System.Drawing.Point() { X = lpRect.Right - 3, Y = lpRect.Bottom - 3 };

            IntPtr leftTopHwnd = MarsWindowsAPIs.WindowFromPoint(leftTop);
            if (leftTopHwnd == IntPtr.Zero)
            {
                iLastError = MarsWindowsAPIs.GetLastError();
                strError = $"WindowFromPoint, leftTop return|{iLastError}|";
                return false;
            }
            if (leftTopHwnd.ToInt64() != hWnd.ToInt64())
            {
                strError = $"WindowFromPoint, leftTop return different hand of window |{iLastError}|";
                currentWindowStatus = MarsWindowsStatus.en_hidden_left_covered;
                return false;
            }
            IntPtr rightTopHwnd = MarsWindowsAPIs.WindowFromPoint(rightTop);
            if (rightTopHwnd == IntPtr.Zero)
            {
                iLastError = MarsWindowsAPIs.GetLastError();
                strError = $"WindowFromPoint, leftTop return|{iLastError}|";
                return false;
            }
            if (rightTopHwnd.ToInt64() != hWnd.ToInt64())
            {
                strError = $"WindowFromPoint, leftTop return different hand of window |{iLastError}|";
                currentWindowStatus = MarsWindowsStatus.en_hidden_left_covered;
                return false;
            }
            IntPtr leftBottomHwnd = MarsWindowsAPIs.WindowFromPoint(leftBottom);
            if (leftBottomHwnd == IntPtr.Zero)
            {
                iLastError = MarsWindowsAPIs.GetLastError();
                strError = $"WindowFromPoint, leftTop return|{iLastError}|";
                return false;
            }
            if (leftBottomHwnd.ToInt64() != hWnd.ToInt64())
            {
                strError = $"WindowFromPoint, leftTop return different hand of window |{iLastError}|";
                currentWindowStatus = MarsWindowsStatus.en_hidden_left_covered;
                return false;
            }
            IntPtr rightBottomHwnd = MarsWindowsAPIs.WindowFromPoint(rightBottom);
            if (rightBottomHwnd == IntPtr.Zero)
            {
                iLastError = MarsWindowsAPIs.GetLastError();
                strError = $"WindowFromPoint, leftTop return|{iLastError}|";
                return false;
            }
            if (rightBottomHwnd.ToInt64() != hWnd.ToInt64())
            {
                strError = $"WindowFromPoint, leftTop return different hand of window |{iLastError}|";
                currentWindowStatus = MarsWindowsStatus.en_hidden_left_covered;
                return false;
            }
            return true;
        }
    }
}
