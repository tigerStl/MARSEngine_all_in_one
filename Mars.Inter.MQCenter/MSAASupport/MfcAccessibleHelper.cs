using Accessibility;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MSAASupport
{
    public static class MfcAccessibleHelper
    {

        [DllImport("oleacc.dll")]
        public static extern int WindowFromAccessibleObject(
           IAccessible pacc,
           out IntPtr phwnd);


        private const int OBJID_WINDOW = 0x00000000;
        private const int OBJID_CLIENT = unchecked((int)0xFFFFFFFC);
        private static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(
            IntPtr hwnd, int dwObjectID, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        //[DllImport("user32.dll")]
        //private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        public static IAccessible? GetIAccessibleFromAfxWindow(IntPtr hwndAfx)
        {
            // 1) 尝试 OBJID_CLIENT
            if (TryGetAcc(hwndAfx, OBJID_CLIENT, out var acc))
                return acc;

            // 2) 退回 OBJID_WINDOW
            if (TryGetAcc(hwndAfx, OBJID_WINDOW, out acc))
                return acc;

            // 3) 枚举子窗口（MFC 常把真实控件挂在 Afx 容器下）
            IAccessible? found = null;
            MarsWindowsAPIs.EnumChildWindows(hwndAfx, (child, _) =>
            {
                if (TryGetAcc(child, OBJID_CLIENT, out var a) || TryGetAcc(child, OBJID_WINDOW, out a))
                {
                    found = a;
                    return false; // 停止枚举
                }
                return true; // 继续
            }, IntPtr.Zero);

            return found;
        }

        private static bool TryGetAcc(IntPtr hwnd, int objId, out IAccessible? acc)
        {
            acc = null;
            try
            {
                if (AccessibleObjectFromWindow(hwnd, objId, ref IID_IAccessible, out var o) >= 0 && o is IAccessible ia)
                {
                    acc = ia;
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static string GetWindowClass(IntPtr h)
        {
            var sb = new StringBuilder(256);
            return MarsWindowsAPIs.GetClassName(h, sb, sb.Capacity) > 0 ? sb.ToString() : string.Empty;
        }

        // 获取某 IAccessible 的第一层子元素（IAccessible 或 childID）
        [DllImport("oleacc.dll")]
        private static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren,
            [Out] object[] rgvarChildren, out int pcObtained);

        public static IEnumerable<object> GetDirectChildren(IAccessible acc)
        {
            int count = 0;
            try { count = acc.accChildCount; } catch { yield break; }
            if (count <= 0) yield break;

            var arr = new object[count];
            if (AccessibleChildren(acc, 0, count, arr, out int obtained) < 0 || obtained <= 0)
                yield break;

            for (int i = 0; i < obtained; i++)
                yield return arr[i];
        }


        public static List<IntPtr> GetDirectChildWindows(IntPtr parentHwnd)
        {
            var childHandles = new List<IntPtr>();
            MarsWindowsAPIs.EnumChildWindows(parentHwnd, (hWnd, lParam) =>
            {
                childHandles.Add(hWnd);
                return true; // 继续枚举
            }, IntPtr.Zero);
            return childHandles;
        }

        // 尝试把 childID 升级为 IAccessible
        public static IAccessible? UpgradeChildId(IAccessible parent, int childId)
        {
            try
            {
                var o = parent.get_accChild(childId);
                return o as IAccessible;
            }
            catch { return null; }
        }

        public static bool IsMfcWindow(IntPtr hwnd)
        {
            var className = GetWindowClass(hwnd);
            return className.StartsWith("Afx:") || className.StartsWith("MFC");
        }

        public static void NavigateAllObjects(IntPtr hwnd)
        {
            var rootAcc = GetIAccessibleFromAfxWindow(hwnd);
            if (rootAcc == null) return;
            void Recur(IAccessible acc, int level)
            {
                string name = string.Empty;
                string role = string.Empty;
                string value = string.Empty;
                try { name = acc.get_accName(0) ?? ""; } catch { }
                try { role = acc.get_accRole(0)?.ToString() ?? ""; } catch { }
                try { value = acc.get_accValue(0) ?? ""; } catch { }
                Console.WriteLine($"{new string(' ', level * 2)}- Name: {name}, Role: {role}, Value: {value}");
                foreach (var child in GetDirectChildren(acc))
                {
                    if (child is IAccessible childAcc)
                    {
                        Recur(childAcc, level + 1);
                    }
                    else if (child is int childId)
                    {
                        var upgraded = UpgradeChildId(acc, childId);
                        if (upgraded != null)
                            Recur(upgraded, level + 1);
                        else
                            Console.WriteLine($"{new string(' ', (level + 1) * 2)}- ChildID: {childId} (not IAccessible)");
                    }
                }
            }
            Recur(rootAcc, 0);
        }
    }
}
