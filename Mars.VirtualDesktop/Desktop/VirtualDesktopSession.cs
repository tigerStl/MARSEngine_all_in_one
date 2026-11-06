using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Mars.VirtualDesktop.Native;

namespace Mars.VirtualDesktop.Desktop;

/// <summary>
/// Owns one Win32 virtual desktop: create, launch processes onto it, capture frames, switch input focus.
/// Capture runs on a dedicated thread that has never created windows (required by SetThreadDesktop).
/// </summary>
internal sealed class VirtualDesktopSession : IDisposable
{
    private readonly object _gate = new();
    private IntPtr _hDesktop = IntPtr.Zero;
    private IntPtr _hDefaultDesktop = IntPtr.Zero;
    private string _desktopName = "";
    private readonly List<int> _launchedPids = new();
    private bool _disposed;

    public string DesktopName => _desktopName;
    public bool IsCreated
    {
        get { lock (_gate) return _hDesktop != IntPtr.Zero; }
    }
    public IReadOnlyList<int> LaunchedProcessIds
    {
        get { lock (_gate) return _launchedPids.ToArray(); }
    }

    public event Action<string>? Log;

    private void WriteLog(string msg)
    {
        Log?.Invoke(msg);
        Debug.WriteLine($"[VirtualDesktopSession] {msg}");
    }

    public void Create(string? preferredName = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_hDesktop != IntPtr.Zero)
                throw new InvalidOperationException($"Desktop already created: {_desktopName}");

            _hDefaultDesktop = Win32DesktopApi.GetThreadDesktop(Win32DesktopApi.GetCurrentThreadId());
            _desktopName = string.IsNullOrWhiteSpace(preferredName)
                ? $"MarsVD_{Environment.ProcessId}_{DateTime.Now:HHmmss}"
                : preferredName.Trim();

            _hDesktop = Win32DesktopApi.CreateDesktopW(
                _desktopName,
                IntPtr.Zero,
                IntPtr.Zero,
                0,
                Win32DesktopApi.DesktopAccessAll,
                IntPtr.Zero);

            if (_hDesktop == IntPtr.Zero)
                throw new InvalidOperationException("CreateDesktopW failed: " + Win32DesktopApi.LastWin32ErrorMessage());

            WriteLog($"Created desktop [{_desktopName}] handle=0x{_hDesktop.ToInt64():X}");
        }
    }

    /// <summary>Launch a process with STARTUPINFO.lpDesktop so its UI appears on this virtual desktop.</summary>
    public int LaunchOnDesktop(string commandLine, string? workingDirectory = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureCreated();
            if (string.IsNullOrWhiteSpace(commandLine))
                throw new ArgumentException("commandLine is empty", nameof(commandLine));

            var si = new Win32DesktopApi.STARTUPINFO
            {
                cb = Marshal.SizeOf<Win32DesktopApi.STARTUPINFO>(),
                lpDesktop = _desktopName,
                dwFlags = 0,
                wShowWindow = (short)Win32DesktopApi.SW_SHOWNORMAL
            };

            bool ok = Win32DesktopApi.CreateProcessW(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                workingDirectory,
                ref si,
                out var pi);

            if (!ok)
                throw new InvalidOperationException("CreateProcessW failed: " + Win32DesktopApi.LastWin32ErrorMessage());

            try
            {
                _launchedPids.Add(pi.dwProcessId);
                WriteLog($"Launched on [{_desktopName}]: pid={pi.dwProcessId} cmd=[{commandLine}]");
                return pi.dwProcessId;
            }
            finally
            {
                if (pi.hThread != IntPtr.Zero) Win32DesktopApi.CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) Win32DesktopApi.CloseHandle(pi.hProcess);
            }
        }
    }

    /// <summary>Make this desktop the interactive (visible / input) desktop for the session.</summary>
    public void SwitchTo()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureCreated();
            if (!Win32DesktopApi.SwitchDesktop(_hDesktop))
                throw new InvalidOperationException("SwitchDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());
            IsInteractiveOnVirtualDesktop = true;
            WriteLog($"Switched interactive desktop to [{_desktopName}] (stays until Switch Back)");
        }
    }

    /// <summary>Switch back to the desktop that was current when Create() ran (usually Default).</summary>
    public void SwitchToDefault()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_hDefaultDesktop == IntPtr.Zero)
                throw new InvalidOperationException("Default desktop handle is not available");
            if (!Win32DesktopApi.SwitchDesktop(_hDefaultDesktop))
                throw new InvalidOperationException("SwitchDesktop(default) failed: " + Win32DesktopApi.LastWin32ErrorMessage());
            IsInteractiveOnVirtualDesktop = false;
            WriteLog("Switched interactive desktop back to default");
        }
    }

    /// <summary>
    /// Full-desktop BitBlt capture. Temporarily SwitchDesktop → BitBlt → SwitchDesktop back to default.
    /// This is the only reliable way to BitBlt a non-input Win32 desktop.
    /// </summary>
    public Bitmap CaptureFrameBitBlt()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureCreated();
            if (_hDefaultDesktop == IntPtr.Zero)
                throw new InvalidOperationException("Default desktop handle is not available");

            if (!Win32DesktopApi.SwitchDesktop(_hDesktop))
                throw new InvalidOperationException("SwitchDesktop(VD) for BitBlt failed: " + Win32DesktopApi.LastWin32ErrorMessage());

            try
            {
                Thread.Sleep(20);
                int w = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CXSCREEN);
                int h = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CYSCREEN);
                var bmp = TryCaptureScreenBitBlt(w, h);
                if (bmp == null)
                    throw new InvalidOperationException("BitBlt returned null after SwitchDesktop: " + Win32DesktopApi.LastWin32ErrorMessage());
                return bmp;
            }
            finally
            {
                if (!Win32DesktopApi.SwitchDesktop(_hDefaultDesktop))
                    WriteLog("WARN: SwitchDesktop(default) after BitBlt failed: " + Win32DesktopApi.LastWin32ErrorMessage());
                IsInteractiveOnVirtualDesktop = false;
            }
        }
    }

    /// <summary>
    /// Capture without switching interactive desktop (PrintWindow composite fallback).
    /// Prefer <see cref="CaptureFrameBitBlt"/> for a full desktop image in the right pane.
    /// </summary>
    public Bitmap CaptureFrame()
    {
        string name;
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureCreated();
            name = _desktopName;
        }

        Bitmap? result = null;
        Exception? captureEx = null;
        var done = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            IntPtr hOpened = IntPtr.Zero;
            try
            {
                // Open by name on this thread — more reliable than sharing CreateDesktop handle across threads.
                hOpened = Win32DesktopApi.OpenDesktopW(name, 0, false, Win32DesktopApi.DesktopAccessAll);
                if (hOpened == IntPtr.Zero)
                    throw new InvalidOperationException("OpenDesktopW failed: " + Win32DesktopApi.LastWin32ErrorMessage());

                if (!Win32DesktopApi.SetThreadDesktop(hOpened))
                    throw new InvalidOperationException("SetThreadDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());

                int w = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CXSCREEN);
                int h = Win32DesktopApi.GetSystemMetrics(Win32DesktopApi.SM_CYSCREEN);
                if (w <= 0 || h <= 0)
                    throw new InvalidOperationException($"Invalid screen metrics {w}x{h}");

                // Prefer window composition; screen BitBlt on a non-input desktop commonly returns code=6.
                result = TryCaptureScreenBitBlt(w, h) ?? CaptureByPrintWindowComposite(hOpened, w, h);
            }
            catch (Exception ex)
            {
                captureEx = ex;
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
            Name = "MarsVD-Capture"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.Wait(TimeSpan.FromSeconds(15)))
            throw new TimeoutException($"Capture of desktop [{name}] timed out");

        if (captureEx != null)
            throw new InvalidOperationException($"Capture failed on [{name}]: {captureEx.Message}", captureEx);

        return result ?? throw new InvalidOperationException("Capture returned null bitmap");
    }

    /// <summary>May return null when the desktop is not the input desktop (BitBlt invalid handle).</summary>
    private static Bitmap? TryCaptureScreenBitBlt(int w, int h)
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

    /// <summary>
    /// Enumerate top-level windows on the current thread desktop and PrintWindow each into a composite bitmap.
    /// Works for inactive Win32 desktops where screen BitBlt fails.
    /// </summary>
    private static Bitmap CaptureByPrintWindowComposite(IntPtr hDesktop, int screenW, int screenH)
    {
        var windows = new List<IntPtr>();
        Win32DesktopApi.EnumDesktopWindows(hDesktop, (hWnd, _) =>
        {
            if (hWnd != IntPtr.Zero && Win32DesktopApi.IsWindowVisible(hWnd))
                windows.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        // Enum order is typically front-to-back; paint back-to-front.
        windows.Reverse();

        var composite = new Bitmap(screenW, screenH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(composite))
        {
            g.Clear(Color.FromArgb(255, 40, 40, 40));
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            if (windows.Count == 0)
            {
                using var font = new Font("Segoe UI", 14f);
                g.DrawString("Virtual desktop has no visible windows yet.\nLaunch an app, then mirror again.",
                    font, Brushes.WhiteSmoke, new RectangleF(24, 24, screenW - 48, 120));
            }

            foreach (IntPtr hwnd in windows)
            {
                if (!Win32DesktopApi.GetWindowRect(hwnd, out var rc))
                    continue;
                int ww = rc.Width;
                int wh = rc.Height;
                if (ww <= 1 || wh <= 1)
                    continue;
                // Skip absurd / off-screen shells
                if (ww > screenW * 2 || wh > screenH * 2)
                    continue;

                using var winBmp = new Bitmap(ww, wh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var wg = Graphics.FromImage(winBmp))
                {
                    wg.Clear(Color.Transparent);
                    IntPtr hdc = wg.GetHdc();
                    try
                    {
                        // Prefer full content; fall back to classic PrintWindow.
                        if (!Win32DesktopApi.PrintWindow(hwnd, hdc, Win32DesktopApi.PW_RENDERFULLCONTENT))
                            Win32DesktopApi.PrintWindow(hwnd, hdc, 0);
                    }
                    finally
                    {
                        wg.ReleaseHdc(hdc);
                    }
                }

                g.DrawImageUnscaled(winBmp, rc.Left, rc.Top);
            }
        }

        return composite;
    }

    /// <summary>True after SwitchTo() until SwitchToDefault() — interactive desktop is the VD.</summary>
    public bool IsInteractiveOnVirtualDesktop { get; private set; }

    internal void MarkInteractive(bool value) => IsInteractiveOnVirtualDesktop = value;

    /// <summary>
    /// Click on the virtual desktop without SwitchDesktop (PostMessage). Preferred for Mirror.
    /// </summary>
    public string ClickAt(int screenX, int screenY, bool rightButton = false)
    {
        string detail = "";
        RunOnDesktopThread(hDesktop =>
        {
            if (!DesktopMessageInput.ClickAt(hDesktop, screenX, screenY, rightButton, out detail))
                throw new InvalidOperationException(detail);
        });
        WriteLog(detail);
        return detail;
    }

    /// <summary>
    /// Type into a window on the virtual desktop without SwitchDesktop (WM_CHAR PostMessage).
    /// </summary>
    public string TypeText(string text)
    {
        string detail = "";
        RunOnDesktopThread(hDesktop =>
        {
            if (!DesktopMessageInput.TypeText(hDesktop, text, out detail))
                throw new InvalidOperationException(detail);
        });
        WriteLog(detail);
        return detail;
    }

    /// <summary>
    /// Legacy: SwitchDesktop → SendInput → SwitchDesktop back. Causes visible snap-back; avoid for Mirror.
    /// </summary>
    public void RunOnInteractiveDesktop(Action action, int settleMs = 200)
    {
        ArgumentNullException.ThrowIfNull(action);
        SwitchTo();
        try
        {
            Thread.Sleep(settleMs);
            action();
            Thread.Sleep(50);
        }
        finally
        {
            SwitchToDefault();
        }
    }

    /// <summary>Open desktop by name on a clean worker thread, SetThreadDesktop, run action with hDesktop.</summary>
    private void RunOnDesktopThread(Action<IntPtr> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        string name;
        lock (_gate)
        {
            ThrowIfDisposed();
            EnsureCreated();
            name = _desktopName;
        }

        Exception? ex = null;
        var done = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            IntPtr hOpened = IntPtr.Zero;
            try
            {
                hOpened = Win32DesktopApi.OpenDesktopW(name, 0, false, Win32DesktopApi.DesktopAccessAll);
                if (hOpened == IntPtr.Zero)
                    throw new InvalidOperationException("OpenDesktopW failed: " + Win32DesktopApi.LastWin32ErrorMessage());
                if (!Win32DesktopApi.SetThreadDesktop(hOpened))
                    throw new InvalidOperationException("SetThreadDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());
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
            Name = "MarsVD-Input"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.Wait(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("Desktop input thread timed out");
        if (ex != null)
            throw new InvalidOperationException(ex.Message, ex);
    }

    public byte[] CapturePngBytes()
    {
        using var bmp = CaptureFrame();
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private void EnsureCreated()
    {
        if (_hDesktop == IntPtr.Zero)
            throw new InvalidOperationException("Virtual desktop has not been created yet");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VirtualDesktopSession));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            if (_hDesktop != IntPtr.Zero)
            {
                Win32DesktopApi.CloseDesktop(_hDesktop);
                WriteLog($"Closed desktop [{_desktopName}]");
                _hDesktop = IntPtr.Zero;
            }
        }
    }
}
