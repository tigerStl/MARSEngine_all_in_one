using System.Diagnostics;
using System.Drawing;
using Mars.VirtualDesktop.Native;

namespace Mars.VirtualDesktop.Desktop;

/// <summary>
/// Manages multiple Win32 virtual desktops created by this process, plus switch-to for any enumerated desktop.
/// </summary>
internal sealed class VirtualDesktopManager : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, VirtualDesktopSession> _owned =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IntPtr> _tempOpenHandles =
        new(StringComparer.OrdinalIgnoreCase);
    private IntPtr _hDefaultDesktop = IntPtr.Zero;
    private string _defaultDesktopName = "Default";
    private VirtualDesktopSession? _selected;
    private string? _selectedName;
    private bool _disposed;

    public event Action<string>? Log;
    public event Action? Changed;

    public VirtualDesktopSession? Selected
    {
        get { lock (_gate) return _selected; }
    }

    /// <summary>Currently selected desktop name (owned or external).</summary>
    public string? SelectedName
    {
        get { lock (_gate) return _selectedName ?? _selected?.DesktopName; }
    }

    public IntPtr DefaultDesktopHandle
    {
        get { lock (_gate) return _hDefaultDesktop; }
    }

    public IReadOnlyCollection<string> OwnedNames
    {
        get { lock (_gate) return _owned.Keys.ToList(); }
    }

    public string DefaultDesktopName
    {
        get { lock (_gate) return _defaultDesktopName; }
    }

    public bool IsInteractiveOnVirtualDesktop
    {
        get
        {
            lock (_gate)
            {
                var snap = DesktopInventory.Capture();
                return !string.Equals(snap.InputDesktopName, _defaultDesktopName, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public VirtualDesktopManager()
    {
        _hDefaultDesktop = Win32DesktopApi.GetThreadDesktop(Win32DesktopApi.GetCurrentThreadId());
        if (_hDefaultDesktop != IntPtr.Zero)
            _defaultDesktopName = Win32DesktopApi.GetDesktopName(_hDefaultDesktop);
    }

    private void WriteLog(string msg)
    {
        Log?.Invoke(msg);
        Debug.WriteLine($"[VirtualDesktopManager] {msg}");
    }

    private void RaiseChanged() => Changed?.Invoke();

    /// <summary>Create a new owned virtual desktop and select it.</summary>
    public VirtualDesktopSession CreateNew(string? preferredName = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var session = new VirtualDesktopSession();
            session.Log += msg => Log?.Invoke(msg);
            session.Create(preferredName);
            if (_owned.ContainsKey(session.DesktopName))
            {
                session.Dispose();
                throw new InvalidOperationException($"Desktop already owned: {session.DesktopName}");
            }
            _owned[session.DesktopName] = session;
            _selected = session;
            WriteLog($"Owned desktops now: {string.Join(", ", _owned.Keys)}");
            RaiseChanged();
            return session;
        }
    }

    public bool TryGetOwned(string name, out VirtualDesktopSession? session)
    {
        lock (_gate)
        {
            if (_owned.TryGetValue(name, out var s))
            {
                session = s;
                return true;
            }
            session = null;
            return false;
        }
    }

    public void Select(string name)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _selectedName = name?.Trim();
            if (!string.IsNullOrEmpty(_selectedName) && _owned.TryGetValue(_selectedName, out var s))
                _selected = s;
            else
                _selected = null; // external / orphaned desktop — still selectable for BitBlt & input
            RaiseChanged();
        }
    }

    public Bitmap CaptureSelectedBitBlt()
    {
        string name = RequireSelectedName();
        lock (_gate)
        {
            if (_owned.TryGetValue(name, out var owned))
                return owned.CaptureFrameBitBlt();
        }
        return DesktopAccessor.CaptureBitBlt(name, DefaultDesktopHandle);
    }

    public string ClickSelected(int x, int y, bool rightButton)
    {
        string name = RequireSelectedName();
        lock (_gate)
        {
            if (_owned.TryGetValue(name, out var owned))
                return owned.ClickAt(x, y, rightButton);
        }
        return DesktopAccessor.ClickAt(name, x, y, rightButton);
    }

    public string TypeSelected(string text)
    {
        string name = RequireSelectedName();
        lock (_gate)
        {
            if (_owned.TryGetValue(name, out var owned))
                return owned.TypeText(text);
        }
        return DesktopAccessor.TypeText(name, text);
    }

    public void KeyPressSelected(ushort vk)
    {
        string name = RequireSelectedName();
        DesktopAccessor.KeyPress(name, vk, DefaultDesktopHandle);
    }

    private string RequireSelectedName()
    {
        lock (_gate)
        {
            string? n = _selectedName ?? _selected?.DesktopName;
            if (string.IsNullOrWhiteSpace(n))
                throw new InvalidOperationException("No desktop selected in the left list.");
            return n;
        }
    }

    /// <summary>Switch interactive desktop to the named desktop (owned or any enumerated).</summary>
    public void SwitchTo(string desktopName)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(desktopName))
                throw new ArgumentException("desktop name empty");

            if (string.Equals(desktopName, _defaultDesktopName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(desktopName, "Default", StringComparison.OrdinalIgnoreCase))
            {
                SwitchToDefault();
                return;
            }

            if (_owned.TryGetValue(desktopName, out var owned))
            {
                owned.SwitchTo();
                _selected = owned;
                WriteLog($"Switched to owned VD [{desktopName}]");
                RaiseChanged();
                return;
            }

            // External / not created by us: OpenDesktop + SwitchDesktop
            IntPtr h = EnsureOpenHandle(desktopName);
            if (!Win32DesktopApi.SwitchDesktop(h))
                throw new InvalidOperationException("SwitchDesktop failed: " + Win32DesktopApi.LastWin32ErrorMessage());
            _selected = null;
            WriteLog($"Switched to desktop [{desktopName}] (not owned by this process)");
            RaiseChanged();
        }
    }

    public void SwitchToDefault()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_hDefaultDesktop == IntPtr.Zero)
                throw new InvalidOperationException("Default desktop handle unavailable");
            if (!Win32DesktopApi.SwitchDesktop(_hDefaultDesktop))
                throw new InvalidOperationException("SwitchDesktop(default) failed: " + Win32DesktopApi.LastWin32ErrorMessage());
            foreach (var s in _owned.Values)
                s.MarkInteractive(false);
            WriteLog($"Switched back to default [{_defaultDesktopName}]");
            RaiseChanged();
        }
    }

    /// <summary>
    /// Close / destroy a desktop: owned Mars sessions, or external/orphaned VDs (not system desktops).
    /// </summary>
    public void CloseDesktop(string desktopName, bool killProcesses = true)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (DesktopAccessor.IsProtectedSystemDesktop(desktopName))
                throw new InvalidOperationException($"Cannot close system desktop [{desktopName}]");

            try { SwitchToDefault(); } catch (Exception ex) { WriteLog("SwitchToDefault before close: " + ex.Message); }
            Thread.Sleep(100);

            if (_tempOpenHandles.TryGetValue(desktopName, out var temp) && temp != IntPtr.Zero)
            {
                Win32DesktopApi.CloseDesktop(temp);
                _tempOpenHandles.Remove(desktopName);
                WriteLog($"Closed temp handle for [{desktopName}]");
            }

            if (_owned.TryGetValue(desktopName, out var session))
            {
                if (killProcesses)
                {
                    foreach (int pid in session.LaunchedProcessIds.ToArray())
                    {
                        try
                        {
                            var p = Process.GetProcessById(pid);
                            if (!p.HasExited)
                            {
                                p.Kill(entireProcessTree: true);
                                p.WaitForExit(2000);
                                WriteLog($"Killed pid={pid} on [{desktopName}]");
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"Kill pid={pid} failed: {ex.Message}");
                        }
                    }
                }

                session.Dispose();
                _owned.Remove(desktopName);
                WriteLog($"Closed owned desktop [{desktopName}]");
            }
            else
            {
                // Orphaned / other-process desktop (e.g. MarsVD_* from a previous run)
                DesktopAccessor.TryDestroyExternal(desktopName, _hDefaultDesktop, WriteLog);
                WriteLog($"Attempted destroy of external desktop [{desktopName}]");
            }

            if (string.Equals(_selectedName, desktopName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(_selected?.DesktopName, desktopName, StringComparison.OrdinalIgnoreCase))
            {
                _selected = _owned.Values.FirstOrDefault();
                _selectedName = _selected?.DesktopName;
            }

            RaiseChanged();
        }
    }

    /// <summary>Backward-compatible alias.</summary>
    public void CloseOwned(string desktopName, bool killProcesses = true) =>
        CloseDesktop(desktopName, killProcesses);

    public bool IsOwned(string name)
    {
        lock (_gate) return _owned.ContainsKey(name);
    }

    private IntPtr EnsureOpenHandle(string desktopName)
    {
        if (_tempOpenHandles.TryGetValue(desktopName, out var existing) && existing != IntPtr.Zero)
            return existing;

        IntPtr h = Win32DesktopApi.OpenDesktopW(desktopName, 0, false, Win32DesktopApi.DesktopAccessAll);
        if (h == IntPtr.Zero)
        {
            var snap = DesktopInventory.Capture();
            throw new InvalidOperationException(
                $"OpenDesktopW([{desktopName}]) failed: {Win32DesktopApi.LastWin32ErrorMessage()}. " +
                $"Known desktops: [{string.Join(", ", snap.Names)}]");
        }
        _tempOpenHandles[desktopName] = h;
        return h;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VirtualDesktopManager));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { SwitchToDefault(); } catch { /* ignore */ }
            foreach (var s in _owned.Values)
                s.Dispose();
            _owned.Clear();
            foreach (var h in _tempOpenHandles.Values)
            {
                if (h != IntPtr.Zero)
                    Win32DesktopApi.CloseDesktop(h);
            }
            _tempOpenHandles.Clear();
            _selected = null;
        }
    }
}
