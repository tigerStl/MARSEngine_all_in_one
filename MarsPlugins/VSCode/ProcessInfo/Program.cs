/*
 * ProcessInfo - Lists running Java processes / Highlight overlay
 * Usage: ProcessInfo [--json] [--stream]
 *        ProcessInfo -highlight <x> <y> <width> <height>  (pixels, screen position; borderless topmost form, flash 3x then close)
 * --stream: output progress to stderr: CHECKING:pid:name | FOUND:pid:display | SKIP:pid
 */

using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;

Log($"ProcessInfo started, args.Length={args?.Length ?? 0}: [{string.Join(" | ", args ?? Array.Empty<string>())}]");

// -highlight a b c d: show borderless topmost form, red border, then set x,y,w,h, flash 3 times, close
for (int i = 0; i < args.Length; i++)
{
    if (string.Equals(args[i], "-highlight", StringComparison.OrdinalIgnoreCase) && i + 4 <= args.Length)
    {
        try
        {
            Log("highlight: args parsed, entering block");
            if (int.TryParse(args[i + 1], out int hx) && int.TryParse(args[i + 2], out int hy) &&
                int.TryParse(args[i + 3], out int hw) && int.TryParse(args[i + 4], out int hh))
            {
                if (hw <= 0) hw = 8; if (hh <= 0) hh = 8;
                Log($"highlight: x y w h = {hx} {hy} {hw} {hh}");
                Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Log("highlight: creating Form");
                var form = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    ShowInTaskbar = false,
                    BackColor = Color.Lime,
                    TransparencyKey = Color.Lime
                };
                form.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.Red, 2);
                    e.Graphics.DrawRectangle(pen, 0, 0, form.ClientSize.Width - 1, form.ClientSize.Height - 1);
                };
                form.Show();
                form.Location = new Point(hx, hy);
                form.Size = new Size(hw, hh);
                Log("highlight: form Show + Location/Size set, starting timer");
                int flashCount = 0;
                var timer = new System.Windows.Forms.Timer { Interval = 250 };
                timer.Tick += (s, e) =>
                {
                    if (flashCount < 6)
                    {
                        form.Visible = (flashCount % 2) == 0;
                        flashCount++;
                        timer.Interval = form.Visible ? 250 : 180;
                    }
                    else
                    {
                        timer.Stop();
                        form.Visible = true;
                        var closeTimer = new System.Windows.Forms.Timer { Interval = 400 };
                        closeTimer.Tick += (_, _) => { closeTimer.Stop(); form.Close(); };
                        closeTimer.Start();
                    }
                };
                form.Shown += (s, e) => { form.Visible = true; flashCount = 0; };
                timer.Start();
                Application.Run(form);
                Log("highlight: Application.Run returned (form closed)");
            }
        }
        catch (Exception ex)
        {
            Log($"highlight: EXCEPTION {ex.Message}");
            Log($"highlight: StackTrace {ex.StackTrace}");
        }
        return;
    }
}

var jsonMode = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
var streamMode = args.Contains("--stream", StringComparer.OrdinalIgnoreCase);
var wsPort = GetArgInt("--ws");

string[] JavaModuleKeywords = { "jvm", "java", "jdk", "jre", "jli", "jimage", "verify" };

// Process names (lowercase) that are always treated as non-Java and skipped early to speed up scanning.
// 系统 / 浏览器 / 常见非 Java 进程前缀，全部小写，按 contains/startsWith 过滤。
string[] ExcludedProcessNameKeywords = new[]
{
    // Windows system & service processes
    "idle", "system", "registry", "smss", "csrss", "wininit", "services",
    "lsaiso", "lsass", "fontdrvhost", "wudfhost", "svchost",
    // 常见安全/驱动相关
    "ekrn",          // ESET
    "syntpenh",      // Synaptics touchpad
    "nvdisplay.container",
    // 浏览器 / 其它非 Java 应用
    "opera", "msedge", "chrome", "dotnet",
    // 其它明确非 Java 服务
    "mqsvc","dllhost", "wslhost", "csrss", "wininit", "services","chrome","msedge","opera","firefox",
    "backgroundTaskHost","msedgewebview2","chromewebview2","chromewebview2embedded","chromewebview2embeddedhost","Cursor"
};

if (wsPort.HasValue)
{
    Log($"INFO: WebSocket port requested: {wsPort.Value}");

    // Quick TCP connect check to see if something is listening on the port.
    try
    {
        using var tcp = new System.Net.Sockets.TcpClient();
        var connectTask = tcp.ConnectAsync("127.0.0.1", wsPort.Value);
        var timeoutTask = Task.Delay(3000);
        var completed = await Task.WhenAny(connectTask, timeoutTask);
        if (completed == connectTask && tcp.Connected)
        {
            Log($"INFO: TCP connect to 127.0.0.1:{wsPort.Value} succeeded");
        }
        else
        {
            Log($"WARN: TCP connect to 127.0.0.1:{wsPort.Value} timed out or failed");
        }
    }
    catch (Exception ex)
    {
        Log($"ERROR: TCP connect to 127.0.0.1:{wsPort.Value} failed - {ex.Message}");
    }

    await RunWebSocketMode(wsPort.Value);
    return;
}

var processes = GetJavaProcesses(null, streamMode);

if (jsonMode)
{
    var json = JsonSerializer.Serialize(processes, new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    Console.WriteLine(json);
}
else
{
    foreach (var p in processes)
    {
        Console.WriteLine($"{p.Pid}: {p.DisplayName}");
    }
}


List<JavaProcessInfo> GetJavaProcesses(object? unused, bool reportChecking)
{
    Log("[START]\tGetJavaProcesses\tINFO: Scanning for Java processes");
    var result = new List<JavaProcessInfo>();
    var seen = new HashSet<int>();
    var jcmdMap = TryGetJavaProcessesFromJcmd();

    try
    {
        var all = Process.GetProcesses();
        var total = all.Length;

        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            try
            {
                if (seen.Contains(p.Id)) continue;

                var name = p.ProcessName ?? "";
                Log($"PROCESSING:{p.Id}:{name}");

                // Filter out common non-Java / system processes early to avoid unnecessary work.
                // 使用 ExcludedProcessNameKeywords 常量（全部小写），按 equals / startsWith / contains 过滤。
                var lname = name.ToLowerInvariant();
                bool isExcluded = false;
                foreach (var ex in ExcludedProcessNameKeywords)
                {
                    if (string.IsNullOrEmpty(ex)) {
                        Log($"SKIP:{p.Id} (excluded:{name})");
                        continue;
                    }
                    if (lname == ex || lname.StartsWith(ex) || lname.Contains(ex))
                    {
                        isExcluded = true;
                        break;
                    }
                }
                if (isExcluded)
                {
                    if (reportChecking) Log($"SKIP:{p.Id} (excluded:{name})");
                    continue;
                }
                if (reportChecking)
                    Log($"CHECKING:{p.Id}:{name}");

                bool isJava = HasJavaModule(p) || jcmdMap.ContainsKey(p.Id);

                string display = name;
                var commandLine = "";
                var mainClass = "";

                if (isJava)
                {
                    seen.Add(p.Id);
                    if (jcmdMap.TryGetValue(p.Id, out var jcmd))
                    {
                        display = jcmd.DisplayName;
                        mainClass = jcmd.MainClass;
                        commandLine = GetCommandLine(p.Id);
                        if (string.IsNullOrWhiteSpace(commandLine))
                        {
                            commandLine = jcmd.DisplayName;
                        }
                    }
                    else
                    {
                        commandLine = GetCommandLine(p.Id);
                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            display = commandLine;
                            mainClass = ExtractMainClass(commandLine);
                        }
                        else
                        {
                            display = $"{name} (PID {p.Id})";
                        }
                    }

                    var displayShort = (display.Length > 80 ? display.Substring(0, 77) + "..." : display).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
                    if (reportChecking)
                        Log($"FOUND:{p.Id}\t{displayShort}");
                }
                else
                {
                    if (reportChecking)
                        Log($"SKIP:{p.Id}");
                }

                if (!isJava) continue;

                result.Add(new JavaProcessInfo
                {
                    Pid = p.Id,
                    MainClass = mainClass,
                    CommandLine = commandLine,
                    DisplayName = display,
                    Source = jcmdMap.ContainsKey(p.Id) ? "jcmd" : "fallback"
                });
            }
            catch(Exception ex)
            {
                Log($"ERROR: Processing PID {p.Id} - {ex.Message}\r\n{ex.StackTrace} ");
                if (reportChecking)
                    Log($"SKIP:{p.Id}");
            }
            finally
            {
                p.Dispose();
            }
        }
    }
    catch (Exception ex)
    {
        Log($"ERROR:{ex.Message}|r\n{ex.StackTrace} ");
    }finally{
        Log($"[END]\tGetJavaProcesses\tINFO: Total Java processes found: {result.Count}");
    }

    return result.OrderBy(x => x.Pid).ToList();
}

Dictionary<int, JavaProcessInfo> TryGetJavaProcessesFromJcmd()
{
    var map = new Dictionary<int, JavaProcessInfo>();
    try
    {
        var jcmdPath = ResolveJcmdExecutable();
        if (string.IsNullOrWhiteSpace(jcmdPath))
        {
            Log("INFO: jcmd not found in JAVA_HOME or PATH, fallback to module/cmdline detection");
            return map;
        }

        var psi = new ProcessStartInfo
        {
            FileName = jcmdPath,
            Arguments = "-l",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Log("WARN: failed to start jcmd process");
            return map;
        }
        if (!proc.WaitForExit(5000))
        {
            try { proc.Kill(true); } catch { }
            Log("WARN: jcmd -l timed out (>5s), fallback to module/cmdline detection");
            return map;
        }

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        if (proc.ExitCode != 0)
        {
            Log($"WARN: jcmd -l exitCode={proc.ExitCode}, stderr={stderr}");
            return map;
        }

        foreach (var rawLine in stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            var firstSpace = line.IndexOf(' ');
            var pidPart = firstSpace > 0 ? line.Substring(0, firstSpace) : line;
            if (!int.TryParse(pidPart, out var pid) || pid <= 0) continue;
            var display = firstSpace > 0 ? line.Substring(firstSpace + 1).Trim() : $"java (PID {pid})";
            if (string.IsNullOrWhiteSpace(display)) display = $"java (PID {pid})";
            map[pid] = new JavaProcessInfo
            {
                Pid = pid,
                MainClass = ExtractMainClassFromJcmdDisplay(display),
                CommandLine = display,
                DisplayName = display
            };
        }
        Log($"INFO: jcmd -l collected {map.Count} process entries");
    }
    catch (Exception ex)
    {
        Log($"WARN: TryGetJavaProcessesFromJcmd failed: {ex.Message}");
    }
    return map;
}

string ResolveJcmdExecutable()
{
    var candidates = new List<string>();
    var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
    if (!string.IsNullOrWhiteSpace(javaHome))
    {
        var file = OperatingSystem.IsWindows() ? "jcmd.exe" : "jcmd";
        candidates.Add(Path.Combine(javaHome, "bin", file));
    }
    candidates.Add("jcmd");

    foreach (var c in candidates)
    {
        try
        {
            if (Path.IsPathRooted(c))
            {
                if (File.Exists(c)) return c;
                continue;
            }
            var psi = new ProcessStartInfo
            {
                FileName = c,
                Arguments = "-h",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) continue;
            if (p.WaitForExit(2000)) return c;
            try { p.Kill(true); } catch { }
        }
        catch
        {
            // try next candidate
        }
    }
    return "";
}

string ExtractMainClassFromJcmdDisplay(string display)
{
    if (string.IsNullOrWhiteSpace(display)) return "";
    var parts = display.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return "";
    var first = parts[0].Trim('"');
    if (first.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            return Path.GetFileNameWithoutExtension(first);
        }
        catch
        {
            return first;
        }
    }
    return first;
}

bool IsJavaProcessByName(string name)
{
    if (string.IsNullOrEmpty(name)) return false;
    var n = name.ToLowerInvariant();
    return n == "java" || n == "javaw" || n == "javaws"
        || n.StartsWith("java")
        || n == "eclipse" || n.StartsWith("eclipse")
        || n == "idea64" || n == "idea" || n.StartsWith("idea")
        || n == "studio64" || n.StartsWith("studio")
        || n == "netbeans" || n.StartsWith("netbeans")
        || n == "jbr" || n.StartsWith("jbr");
}

bool HasJavaModule(Process process)
{
    try
    {
        foreach (ProcessModule? mod in process.Modules)
        {
            if (mod?.ModuleName == null) continue;
            var name = mod.ModuleName.ToLowerInvariant();
            foreach (var kw in JavaModuleKeywords)
            {
                if (name.Contains(kw)) return true;
            }
        }
    }
    catch
    {
        // 32-bit cannot enumerate 64-bit process modules and vice versa
    }
    return false;
}

string GetCommandLine(int pid)
{
    try
    {
        if (OperatingSystem.IsWindows())
        {
            return GetCommandLineWindows(pid);
        }
        return GetCommandLineUnix(pid);
    }
    catch
    {
        return "";
    }
}

string GetCommandLineWindows(int pid)
{
    try
    {
        using var searcher = new System.Management.ManagementObjectSearcher(
            $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}");
        using var results = searcher.Get();
        var proc = results.Cast<System.Management.ManagementObject>().FirstOrDefault();
        return proc?["CommandLine"]?.ToString() ?? "";
    }
    catch
    {
        return "";
    }
}

string GetCommandLineUnix(int pid)
{
    try
    {
        var path = $"/proc/{pid}/cmdline";
        if (!File.Exists(path))
            return "";
        var bytes = File.ReadAllBytes(path);
        return string.Join(" ", System.Text.Encoding.UTF8.GetString(bytes).Split('\0'));
    }
    catch
    {
        return "";
    }
}

string ExtractMainClass(string cmdLine)
{
    var parts = cmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    bool nextIsClass = false;
    foreach (var p in parts)
    {
        if (p == "-jar" || p == "--jar")
        {
            nextIsClass = false;
            continue;
        }
        if (nextIsClass || (!p.StartsWith("-") && p.Contains(".")))
        {
            return p.Trim('"');
        }
        if (p == "-cp" || p == "-classpath" || p == "--class-path")
        {
            nextIsClass = true;
        }
    }
    return "";
}

int? GetArgInt(string name)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var val))
                return val;
        }
    }
    return null;
}

void Log(string message)
{
    try
    {
        Console.Error.WriteLine(message);
        var dir = Path.Combine("C:\\", "temp");
        Directory.CreateDirectory(dir);
        var ts = DateTime.Now.ToString("yyyyMMddHHmm");
        var file = Path.Combine(dir, $"marsExtensionjava{ts}.log");
        var line = DateTime.Now.ToString("o") + " " + message + Environment.NewLine;
        File.AppendAllText(file, line, Encoding.UTF8);
    }
    catch
    {
        // Swallow any logging errors to avoid affecting main flow
    }
}

async Task RunWebSocketMode(int port)
{
    using var ws = new ClientWebSocket();
    var uri = new Uri($"ws://127.0.0.1:{port}/");
    try
    {
        await ws.ConnectAsync(uri, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Log($"ERROR: Failed to connect to ws://127.0.0.1:{port} - {ex.Message}");
        return;
    }
    // Log and print when the WebSocket connection is established
    Log($"INFO: WebSocket connected to ws://127.0.0.1:{port}");
    try { Console.WriteLine($"CONNECTED:127.0.0.1:{port}"); } catch { }

    // Serve multiple incoming requests until the remote closes the connection.
    while (ws.State == WebSocketState.Open)
    {
        string commandText;
        try
        {
            commandText = await ReceiveText(ws, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log($"ERROR: Receive failed - {ex.Message}");
            break;
        }

        if (string.IsNullOrWhiteSpace(commandText))
        {
            // remote closed or empty message
            break;
        }

        // Log the received packet contents (truncate large payloads and escape newlines)
        try
        {
            var max = 2000;
            var snippet = commandText.Length > max ? commandText.Substring(0, max) + "..." : commandText;
            snippet = snippet.Replace("\r", "\\r").Replace("\n", "\\n");
            Log($"RECV_LEN:{commandText.Length} RECV_SNIPPET:{snippet}");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Failed to log received packet - {ex.Message}");
        }

        string? acquireId = null;
        string? command = null;

        try
        {
            using var doc = JsonDocument.Parse(commandText);
            if (doc.RootElement.TryGetProperty("acquireId", out var a))
                acquireId = a.ToString();
            if (doc.RootElement.TryGetProperty("command", out var c))
                command = c.GetString();
        }
        catch (Exception ex)
        {
            Log($"ERROR: Invalid command JSON received - {ex.Message}");
            continue;
        }

        if (!string.Equals(command, "scan_java_process", StringComparison.OrdinalIgnoreCase))
            continue;

        // Find Java processes and send to extension via WebSocket
        var processes = GetJavaProcesses(null, false);
        Log($"INFO: Sending {processes.Count} Java process(es) to extension");
        var response = new
        {
            acquireId,
            from = "dotnetProcess",
            javaProcess = processes.Select(p => new { displayName = p.DisplayName, pid = p.Pid, mainClass = p.MainClass, source = p.Source }).ToList()
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        try
        {
            using var sendCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await SendText(ws, json, sendCts.Token);
            Log("INFO: Java process list sent to extension successfully");
        }
        catch (OperationCanceledException)
        {
            Log("ERROR: WebSocket send timed out sending response");
        }
        catch (Exception ex)
        {
            Log($"ERROR: Failed to send response - {ex.Message}");
        }
        // continue waiting for next request
    }
    Log($"INFO: WebSocket loop exiting for ws://127.0.0.1:{port}");
}

static async Task<string> ReceiveText(ClientWebSocket ws, CancellationToken ct)
{
    var buffer = new byte[4096];
    using var ms = new MemoryStream();
    while (true)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
        if (result.MessageType == WebSocketMessageType.Close)
            return "";
        ms.Write(buffer, 0, result.Count);
        if (result.EndOfMessage)
            break;
    }
    return Encoding.UTF8.GetString(ms.ToArray());
}

static async Task SendText(ClientWebSocket ws, string text, CancellationToken ct)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
}

public record JavaProcessInfo
{
    public int Pid { get; init; }
    public string? MainClass { get; init; }
    public string? CommandLine { get; init; }
    public string DisplayName { get; init; } = "";
    public string? Source { get; init; }
}
