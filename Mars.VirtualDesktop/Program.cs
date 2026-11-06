using System.Windows.Forms;
using Mars.VirtualDesktop.Desktop;
using Mars.VirtualDesktop.Host;
using Mars.VirtualDesktop.UI;
using Microsoft.Extensions.Hosting;

namespace Mars.VirtualDesktop;

/// <summary>
/// Mars subsystem EXE (Core / net8.0-windows).
/// Multi virtual-desktop manager: create / list / switch / close / mirror / input.
/// Optional HTTP control via --port.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "Mars.VirtualDesktop UI error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            MessageBox.Show(e.ExceptionObject?.ToString() ?? "Unknown error", "Mars.VirtualDesktop fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);

        BootLog("Main begin");

        int port = ParsePort(args);
        bool autoCreate = HasFlag(args, "--create");
        string? launchCmd = ParseValue(args, "--launch");
        string? desktopName = ParseValue(args, "--name");

        VirtualDesktopManager? manager = null;
        IHost? httpHost = null;

        try
        {
            BootLog("Creating VirtualDesktopManager");
            manager = new VirtualDesktopManager();

            if (autoCreate)
            {
                var s = manager.CreateNew(desktopName);
                BootLog($"created desktop={s.DesktopName}");
            }

            if (!string.IsNullOrWhiteSpace(launchCmd))
            {
                if (manager.Selected == null)
                    manager.CreateNew(desktopName);
                int pid = manager.Selected!.LaunchOnDesktop(launchCmd!);
                BootLog($"launched pid={pid} cmd={launchCmd}");
            }

            if (port > 0)
            {
                BootLog($"Starting HTTP on {port}");
                httpHost = ControlServer.Start(port, manager, BootLog);
                httpHost.Start();
            }

            BootLog("Creating MainForm");
            var form = new MainForm(manager);
            BootLog("Application.Run");
            Application.Run(form);
            BootLog("Application.Run exited");
        }
        catch (Exception ex)
        {
            BootLog("FATAL: " + ex);
            MessageBox.Show(ex.ToString(), "Mars.VirtualDesktop failed to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { httpHost?.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult(); }
            catch { /* ignore */ }
            httpHost?.Dispose();
            manager?.Dispose();
        }
    }

    private static void BootLog(string msg)
    {
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "Mars.VirtualDesktop.boot.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }
        catch { /* ignore */ }
    }

    private static int ParsePort(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] is "--port" or "-port") && i + 1 < args.Length
                && int.TryParse(args[i + 1], out int p) && p > 0)
                return p;
        }
        return 0;
    }

    private static string? ParseValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    private static bool HasFlag(string[] args, string name) =>
        args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
}
