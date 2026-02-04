using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public class MFCAndStandardComboboxHelper
    {
        private const int CB_GETLBTEXT = 0x0148;
        private const int CB_GETITEMRECT = 0x0152;

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, StringBuilder lParam);

        /// <summary>
        /// 获取ComboBox指定项的文本
        /// </summary>
        public static string GetComboBoxItemText(IntPtr comboHwnd, int index)
        {
            StringBuilder sb = new StringBuilder(256);
            MarsWindowsAPIs.SendMessage(comboHwnd, CB_GETLBTEXT, (IntPtr)index, sb);
            return sb.ToString();
        }

        /// <summary>
        /// 获取ComboBox指定项的矩形区域（相对于ComboBox控件）
        /// </summary>
        public static MarsWindowsAPIs.RECT GetComboBoxItemRect(IntPtr comboHwnd, int index)
        {
            MarsWindowsAPIs.RECT rect = new MarsWindowsAPIs.RECT();
            MarsWindowsAPIs.SendMessage(comboHwnd, CB_GETITEMRECT, (IntPtr)index, ref rect);
            return rect;
        }
    }
}
