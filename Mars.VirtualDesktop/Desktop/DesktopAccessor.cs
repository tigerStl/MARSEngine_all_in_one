using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Mars.VirtualDesktop.Native;

namespace Mars.VirtualDesktop.Desktop;

/// <summary>
/// Operate on any Win32 desktop by name (owned or orphaned / external): BitBlt, click, type.
/// </summary>
internal static class DesktopAccessor
{
    public static Bitmap CaptureBitBlt(string desktopName, IntPtr hDefaultDesktop)
    {
        if (string.IsNullOrWhiteSpace(desktopName))
            throw new ArgumentException("desktop name empty");
        if (hDefaultDesktop == IntPtr.Zero)
            throw new ArgumentException("default desktop handle required");

        IntPtr hDesk = Win32DesktopApi.OpenDesktopW(desktopName, 0, false, Win32DesktopApi.DesktopAccessAll);
        if (hDesk == IntPtr.Zero)
            throw new InvalidOperationException(
                $"OpenDesktopW([{desktopName}]) failed: " + Win32DesktopApi.LastWin32ErrorMessage());

        try
        {
            if (!Win32DesktopApi.SwitchDesktop(hDesk))
                throw new InvalidOperationException(
                    $"SwitchDesktop([{desktopName}]) failed: " + Win32DesktopApi.LastWin32ErrorMessage());

            try
            {
                Thread.Sleep(20);
                int w = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CXSCREEN);
                int h = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CYSCREEN);
                var bmp = BitBltScreen(w, h);
                if (bmp == null)
                    throw new InvalidOperationException("BitBlt failed: " + Win32DesktopApi.LastWin32ErrorMessage());
                return bmp;
            }
            finally
            {
                Win32DesktopApi.SwitchDesktop(hDefaultDesktop);
            }
        }
        finally
        {
            Win32DesktopApi.CloseDesktop(hDesk);
        }
    }

    public static string ClickAt(string desktopName, int screenX, int screenY, bool rightButton)
    {
        string detail = "";
        RunOnDesktop(desktopName, h =>
        {
            if (!DesktopMessageInput.ClickAt(h, screenX, screenY, rightButton, out detail))
                throw new InvalidOperationException(detail);
        });
        return detail;
    }

    public static string TypeText(string desktopName, string text)
    {
        string detail = "";
        RunOnDesktop(desktopName, h =>
        {
            if (!DesktopMessageInput.TypeText(h, text, out detail))
                throw new InvalidOperationException(detail);
        });
        return detail;
    }

    public static void KeyPress(string desktopName, ushort vk, IntPtr hDefaultDesktop)
    {
        IntPtr hDesk = Win32DesktopApi.OpenDesktopW(desktopName, 0, false, Win32DesktopApi.DesktopAccessAll);
        if (hDesk == IntPtr.Zero)
            throw new InvalidOperationException("OpenDesktopW failed: " + Win32DesktopApi.LastWin32ErrorMessage());
        try
        {
            if (!Win32DesktopApi.SwitchDesktop(hDesk))
                throw new InvalidOperationException("SwitchDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());
            try
            {
                Thread.Sleep(20);
                InputInjector.KeyPress(vk);
                Thread.Sleep(20);
            }
            finally
            {
                Win32DesktopApi.SwitchDesktop(hDefaultDesktop);
            }
        }
        finally
        {
            Win32DesktopApi.CloseDesktop(hDesk);
        }
    }

    /// <summary>
    /// Best-effort destroy of a desktop we don't own: kill processes with windows on it, then drop our handle.
    /// System desktops (Default/Winlogon/Disconnect) are refused.
    /// </summary>
    public static void TryDestroyExternal(string desktopName, IntPtr hDefaultDesktop, Action<string>? log = null)
    {
        string n = desktopName.Trim();
        if (IsProtectedSystemDesktop(n))
            throw new InvalidOperationException($"Refusing to destroy system desktop [{n}]");

        // Ensure we're not viewing it as input
        Win32DesktopApi.SwitchDesktop(hDefaultDesktop);
        Thread.Sleep(80);

        var pids = new HashSet<int>();
        RunOnDesktop(n, h =>
        {
            Win32DesktopApi.EnumDesktopWindows(h, (hwnd, _lp) =>
            {
                if (hwnd == IntPtr.Zero) return true;
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid > 0 && pid != (uint)Environment.ProcessId)
                    pids.Add((int)pid);
                return true;
            }, IntPtr.Zero);
        });

        foreach (int pid in pids)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                if (!p.HasExited)
                {
                    p.Kill(entireProcessTree: true);
                    p.WaitForExit(2000);
                    log?.Invoke($"Killed pid={pid} on external desktop [{n}]");
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"Kill pid={pid} failed: {ex.Message}");
            }
        }

        // Open and immediately close — if we were last handle and no threads remain, desktop goes away.
        IntPtr hClose = Win32DesktopApi.OpenDesktopW(n, 0, false, Win32DesktopApi.DesktopAccessAll);
        if (hClose != IntPtr.Zero)
        {
            Win32DesktopApi.CloseDesktop(hClose);
            log?.Invoke($"Closed handle for [{n}] (desktop removed if no other handles remain)");
        }
    }

    public static bool IsProtectedSystemDesktop(string name)
    {
        string n = name.Trim();
        return n.Equals("Default", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Winlogon", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Disconnect", StringComparison.OrdinalIgnoreCase)
            || n.Equals("WinSta0", StringComparison.OrdinalIgnoreCase);
    }

    private static void RunOnDesktop(string desktopName, Action<IntPtr> action)
    {
        IntPtr hOpened = IntPtr.Zero;
        Exception? ex = null;
        var done = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            try
            {
                hOpened = Win32DesktopApi.OpenDesktopW(desktopName, 0, false, Win32DesktopApi.DesktopAccessAll);
                if (hOpened == IntPtr.Zero)
                    throw new InvalidOperationException(
                        $"OpenDesktopW([{desktopName}]) failed: " + Win32DesktopApi.LastWin32ErrorMessage());
                if (!Win32DesktopApi.SetThreadDesktop(hOpened))
                    throw new InvalidOperationException(
                        "SetThreadDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());
                action(hOpened);
            }
            catch (Exception e)
            {
                ex = e;
            }
            finally
            {
                if (hOpened != IntPtr.Zero)
                    Win32DesktopApi.CloseDesktop(hOpened);
                done.Set();
            }
        })
        {
            IsBackground = true,
            Name = "MarsVD-Accessor"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.Wait(TimeSpan.FromSeconds(12)))
            throw new TimeoutException($"DesktopAccessor timed out on [{desktopName}]");
        if (ex != null)
            throw new InvalidOperationException(ex.Message, ex);
    }

    private static Bitmap? BitBltScreen(int w, int h)
    {
        IntPtr hwndDesk = Win32DesktopApi.GetDesktopWindow();
        IntPtr hdcScreen = Win32DesktopApi.GetWindowDC(hwndDesk);
        if (hdcScreen == IntPtr.Zero)
            hdcScreen = Win32DesktopApi.GetDC(IntPtr.Zero);
        if (hdcScreen == IntPtr.Zero)
            return null;

        IntPtr hdcMem = IntPtr.Zero;
        IntPtr hBmp = IntPtr.Zero;
        IntPtr hOld = IntPtr.Zero;
        try
        {
            hdcMem = Win32DesktopApi.CreateCompatibleDC(hdcScreen);
            if (hdcMem == IntPtr.Zero) return null;
            hBmp = Win32DesktopApi.CreateCompatibleBitmap(hdcScreen, w, h);
            if (hBmp == IntPtr.Zero) return null;
            hOld = Win32DesktopApi.SelectObject(hdcMem, hBmp);
            if (!Win32DesktopApi.BitBlt(hdcMem, 0, 0, w, h, hdcScreen, 0, 0, Win32DesktopApi.SRCCOPY))
                return null;
            return Image.FromHbitmap(hBmp);
        }
        finally
        {
            if (hOld != IntPtr.Zero) Win32DesktopApi.SelectObject(hdcMem, hOld);
            if (hBmp != IntPtr.Zero) Win32DesktopApi.DeleteObject(hBmp);
            if (hdcMem != IntPtr.Zero) Win32DesktopApi.DeleteDC(hdcMem);
            Win32DesktopApi.ReleaseDC(hwndDesk, hdcScreen);
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
