using Mars.VirtualDesktop.Native;

namespace Mars.VirtualDesktop.Desktop;

/// <summary>
/// Enumerate Win32 desktops on the current window station (includes CreateDesktop virtual desktops).
/// Note: Windows 10/11 Task View desktops are a different COM concept and are not listed here.
/// </summary>
internal static class DesktopInventory
{
    public sealed class Snapshot
    {
        public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();
        public string InputDesktopName { get; init; } = "";
        public string ThreadDesktopName { get; init; } = "";
        public int Count => Names.Count;

        public string FormatBanner(string? sessionDesktopName = null)
        {
            string ours = string.IsNullOrEmpty(sessionDesktopName) ? "" : $", session=[{sessionDesktopName}]";
            string names = Names.Count == 0 ? "(none)" : string.Join(", ", Names.Select(FormatName));
            return $"Win32 desktops: {Count}  |  input=[{InputDesktopName}]  thread=[{ThreadDesktopName}]{ours}  |  {names}";
        }

        private string FormatName(string name)
        {
            bool isInput = string.Equals(name, InputDesktopName, StringComparison.OrdinalIgnoreCase);
            bool isThread = string.Equals(name, ThreadDesktopName, StringComparison.OrdinalIgnoreCase);
            if (isInput && isThread) return $"{name}*";
            if (isInput) return $"{name} (input)";
            if (isThread) return $"{name} (thread)";
            return name;
        }
    }

    public static Snapshot Capture()
    {
        var names = new List<string>();
        IntPtr hWinsta = Win32DesktopApi.GetProcessWindowStation();
        if (hWinsta != IntPtr.Zero)
        {
            Win32DesktopApi.EnumDesktopsW(hWinsta, (desktopName, _) =>
            {
                if (!string.IsNullOrWhiteSpace(desktopName))
                    names.Add(desktopName);
                return true;
            }, IntPtr.Zero);
        }

        names.Sort(StringComparer.OrdinalIgnoreCase);

        string inputName = "(unknown)";
        IntPtr hInput = Win32DesktopApi.OpenInputDesktop(0, false, Win32DesktopApi.DESKTOP_READOBJECTS);
        if (hInput != IntPtr.Zero)
        {
            try { inputName = Win32DesktopApi.GetDesktopName(hInput); }
            finally { Win32DesktopApi.CloseDesktop(hInput); }
        }

        string threadName = "(unknown)";
        IntPtr hThread = Win32DesktopApi.GetThreadDesktop(Win32DesktopApi.GetCurrentThreadId());
        if (hThread != IntPtr.Zero)
            threadName = Win32DesktopApi.GetDesktopName(hThread);

        return new Snapshot
        {
            Names = names,
            InputDesktopName = inputName,
            ThreadDesktopName = threadName
        };
    }
}
