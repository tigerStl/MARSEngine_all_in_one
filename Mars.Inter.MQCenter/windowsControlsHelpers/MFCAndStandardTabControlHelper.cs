using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public class MFCAndStandardTabControlHelper
    {
        private const int TCM_GETITEMRECT = 0x130A;
        const int MAX_TEXT = 256;

        /// <summary>
        /// 获取指定Tab页的矩形区域（相对于Tab控件）
        /// </summary>
        public static MarsWindowsAPIs.RECT GetTabItemRect(IntPtr tabHwnd, int index)
        {
            MarsWindowsAPIs.RECT rect = new MarsWindowsAPIs.RECT();
            MarsWindowsAPIs.SendMessage(tabHwnd, TCM_GETITEMRECT, (IntPtr)index, ref rect);
            return rect;
        }

        #region tab 相关
        public static string GetTabItemText(IntPtr tabHwnd, int index)
        {            
            StringBuilder sb = new StringBuilder(MAX_TEXT);
            MarsWindowsAPIs.TCITEM item = new MarsWindowsAPIs.TCITEM
            {
                mask = MarsWindowsAPIs.TCIF_TEXT,
                pszText = Marshal.AllocHGlobal(MAX_TEXT * 2),
                cchTextMax = MAX_TEXT
            };

            try
            {
                Marshal.Copy(sb.ToString().ToCharArray(), 0, item.pszText, 0);
                var rslt = MarsWindowsAPIs.SendMessage(tabHwnd, MarsWindowsAPIs.TCM_GETITEMW, (IntPtr)index, ref item);
                string result = Marshal.PtrToStringUni(item.pszText);
                return result?.Trim() ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(item.pszText);
            }
        }
        #endregion
    }
}
