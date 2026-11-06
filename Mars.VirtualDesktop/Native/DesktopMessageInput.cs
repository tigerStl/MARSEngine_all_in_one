using System.Runtime.InteropServices;
using System.Text;

namespace Mars.VirtualDesktop.Native;

/// <summary>
/// Inject mouse/keyboard into windows on a (possibly inactive) Win32 desktop via PostMessage.
/// Hit-test uses EnumDesktopWindows (WindowFromPoint often returns null on non-input desktops).
/// </summary>
internal static class DesktopMessageInput
{
    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONDOWN = 0x0204;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CHAR = 0x0102;
    private const uint WM_ACTIVATE = 0x0006;
    private const uint WA_CLICKACTIVE = 2;
    private const uint MK_LBUTTON = 0x0001;
    private const uint MK_RBUTTON = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr RealChildWindowFromPoint(IntPtr hwndParent, POINT ptParentClientCoords);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private static IntPtr MakeLParam(int lo, int hi) =>
        (IntPtr)((hi << 16) | (lo & 0xFFFF));

    private sealed class DesktopWindowInfo
    {
        public IntPtr Hwnd;
        public Win32DesktopApi.RECT Rect;
        public string ClassName = "";
        public string Title = "";
    }

    /// <summary>
    /// Must run on a thread that already called SetThreadDesktop(hDesktop).
    /// </summary>
    public static bool ClickAt(IntPtr hDesktop, int screenX, int screenY, bool rightButton, out string detail)
    {
        var windows = EnumVisibleWindows(hDesktop);
        IntPtr hwnd = HitTestFromList(windows, screenX, screenY, out bool usedFallback, out string hitInfo);

        if (hwnd == IntPtr.Zero)
        {
            detail = $"No window at ({screenX},{screenY}). Visible windows on desktop: {DescribeWindows(windows)}";
            return false;
        }

        // Activate target (best-effort on hidden desktop)
        SendMessage(hwnd, WM_ACTIVATE, (IntPtr)WA_CLICKACTIVE, IntPtr.Zero);
        try { SetForegroundWindow(hwnd); } catch { /* ignore */ }

        var pt = new POINT(screenX, screenY);
        if (!ScreenToClient(hwnd, ref pt))
            pt = new POINT(0, 0);

        // If fallback (click outside all windows), aim at client center
        if (usedFallback)
        {
            if (Win32DesktopApi.GetWindowRect(hwnd, out var rc) && rc.Width > 0 && rc.Height > 0)
            {
                pt = new POINT(rc.Width / 2, rc.Height / 2);
            }
        }
        else
        {
            IntPtr child = RealChildWindowFromPoint(hwnd, pt);
            if (child != IntPtr.Zero && child != hwnd)
            {
                pt = new POINT(screenX, screenY);
                ScreenToClient(child, ref pt);
                hwnd = child;
            }
        }

        // Clamp into client area
        if (Win32DesktopApi.GetWindowRect(hwnd, out var wr))
        {
            int cw = Math.Max(1, wr.Width);
            int ch = Math.Max(1, wr.Height);
            // GetClientRect would be better; approximate with window size if ScreenToClient went negative
            if (pt.X < 0) pt.X = 0;
            if (pt.Y < 0) pt.Y = 0;
            if (pt.X >= cw) pt.X = cw - 1;
            if (pt.Y >= ch) pt.Y = ch - 1;
        }

        IntPtr lp = MakeLParam(pt.X, pt.Y);
        uint down = rightButton ? WM_RBUTTONDOWN : WM_LBUTTONDOWN;
        uint up = rightButton ? WM_RBUTTONUP : WM_LBUTTONUP;
        IntPtr mk = (IntPtr)(rightButton ? MK_RBUTTON : MK_LBUTTON);

        PostMessage(hwnd, WM_MOUSEMOVE, IntPtr.Zero, lp);
        PostMessage(hwnd, down, mk, lp);
        PostMessage(hwnd, up, IntPtr.Zero, lp);

        detail =
            $"PostMessage {(rightButton ? "R" : "L")}Click hwnd=0x{hwnd.ToInt64():X} client=({pt.X},{pt.Y})" +
            (usedFallback ? " [fallback=topVisible]" : "") +
            $" | {hitInfo}";
        return true;
    }

    /// <summary>Must run on a thread that already SetThreadDesktop'd the target desktop.</summary>
    public static bool TypeText(IntPtr hDesktop, string text, out string detail)
    {
        var windows = EnumVisibleWindows(hDesktop);
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || windows.All(w => w.Hwnd != hwnd))
            hwnd = windows.Count > 0 ? windows[0].Hwnd : IntPtr.Zero;

        if (hwnd == IntPtr.Zero)
        {
            detail = $"No window to receive text. Visible: {DescribeWindows(windows)}";
            return false;
        }

        SendMessage(hwnd, WM_ACTIVATE, (IntPtr)WA_CLICKACTIVE, IntPtr.Zero);
        foreach (char ch in text)
            PostMessage(hwnd, WM_CHAR, (IntPtr)ch, IntPtr.Zero);

        detail = $"PostMessage WM_CHAR x{text.Length} → hwnd=0x{hwnd.ToInt64():X}";
        return true;
    }

    private static List<DesktopWindowInfo> EnumVisibleWindows(IntPtr hDesktop)
    {
        var list = new List<DesktopWindowInfo>();
        Win32DesktopApi.EnumDesktopWindows(hDesktop, (hWnd, _) =>
        {
            if (hWnd == IntPtr.Zero || !Win32DesktopApi.IsWindowVisible(hWnd))
                return true;
            if (!Win32DesktopApi.GetWindowRect(hWnd, out var rc))
                return true;
            if (rc.Width <= 1 || rc.Height <= 1)
                return true;

            var cls = new StringBuilder(256);
            GetClassName(hWnd, cls, cls.Capacity);
            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);

            string className = cls.ToString();
            // Skip typical non-app shells if present
            if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd")
                return true;

            list.Add(new DesktopWindowInfo
            {
                Hwnd = hWnd,
                Rect = rc,
                ClassName = className,
                Title = title.ToString()
            });
            return true;
        }, IntPtr.Zero);

        // EnumDesktopWindows is typically front-to-back already; keep that for hit-test (first match wins).
        return list;
    }

    private static IntPtr HitTestFromList(
        List<DesktopWindowInfo> windows, int screenX, int screenY,
        out bool usedFallback, out string hitInfo)
    {
        usedFallback = false;
        hitInfo = "";

        foreach (var w in windows)
        {
            var rc = w.Rect;
            if (screenX >= rc.Left && screenX < rc.Right && screenY >= rc.Top && screenY < rc.Bottom)
            {
                hitInfo = $"hit [{w.ClassName}] \"{Truncate(w.Title, 40)}\" rect=({rc.Left},{rc.Top})-({rc.Right},{rc.Bottom})";
                return w.Hwnd;
            }
        }

        // Point missed every window (empty area / mapping offset). Fall back to topmost visible app window.
        if (windows.Count > 0)
        {
            usedFallback = true;
            var top = windows[0];
            hitInfo =
                $"miss ({screenX},{screenY}); fallback top [{top.ClassName}] \"{Truncate(top.Title, 40)}\" " +
                $"rect=({top.Rect.Left},{top.Rect.Top})-({top.Rect.Right},{top.Rect.Bottom}); " +
                $"all={DescribeWindows(windows)}";
            return top.Hwnd;
        }

        hitInfo = "no visible windows";
        return IntPtr.Zero;
    }

    private static string DescribeWindows(List<DesktopWindowInfo> windows)
    {
        if (windows.Count == 0) return "(none)";
        return string.Join("; ", windows.Select(w =>
            $"0x{w.Hwnd.ToInt64():X}[{w.ClassName}]\"{Truncate(w.Title, 24)}\"" +
            $"({w.Rect.Left},{w.Rect.Top},{w.Rect.Width}x{w.Rect.Height})"));
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "…");
}
