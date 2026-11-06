using System.Text;
using System.Text.Json;
using Mars.VirtualDesktop.Desktop;
using Mars.VirtualDesktop.Native;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mars.VirtualDesktop.Host;

/// <summary>
/// Lightweight HTTP control surface for Mars subsystem automation.
/// GET /  → ready
/// POST /invoke  JSON { "action": "...", "payload": { ... } }
/// </summary>
internal static class ControlServer
{
    public static IHost Start(int port, VirtualDesktopManager manager, Action<string> log)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.MapGet("/", () => Results.Text("Mars.VirtualDesktop: ready", "text/plain"));

        app.MapGet("/status", () =>
        {
            var snap = DesktopInventory.Capture();
            var sel = manager.Selected;
            return Results.Json(new
            {
                ok = true,
                desktopCount = snap.Count,
                desktops = snap.Names,
                owned = manager.OwnedNames,
                input = snap.InputDesktopName,
                selected = sel?.DesktopName,
                pids = sel?.LaunchedProcessIds
            });
        });

        app.MapPost("/invoke", async (HttpContext http) =>
        {
            using var reader = new StreamReader(http.Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync().ConfigureAwait(false);
            log($"HTTP /invoke body={raw}");

            if (string.IsNullOrWhiteSpace(raw))
                return Results.BadRequest(new { ok = false, error = "empty body" });

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var action = root.TryGetProperty("action", out var a) ? a.GetString()
                : root.TryGetProperty("Action", out var a2) ? a2.GetString() : null;
            JsonElement payload = default;
            bool hasPayload = root.TryGetProperty("payload", out payload)
                || root.TryGetProperty("Payload", out payload);

            if (string.IsNullOrWhiteSpace(action))
                return Results.BadRequest(new { ok = false, error = "missing action" });

            try
            {
                var result = Dispatch(manager, action!.Trim().ToLowerInvariant(), hasPayload ? payload : default, log);
                return Results.Json(result);
            }
            catch (Exception ex)
            {
                log($"HTTP /invoke error: {ex.Message}");
                return Results.Json(new { ok = false, error = ex.Message });
            }
        });

        log($"HTTP control listening on http://127.0.0.1:{port}/  (POST /invoke)");
        return app;
    }

    private static VirtualDesktopSession RequireSelected(VirtualDesktopManager manager) =>
        manager.Selected ?? throw new InvalidOperationException("No selected owned desktop — create/select first");

    private static object Dispatch(VirtualDesktopManager manager, string action, JsonElement payload, Action<string> log)
    {
        switch (action)
        {
            case "create":
            case "createdesktop":
            {
                string? name = GetString(payload, "name");
                var s = manager.CreateNew(name);
                return new { ok = true, desktopName = s.DesktopName, owned = manager.OwnedNames };
            }
            case "launch":
            case "start":
            {
                string cmd = GetString(payload, "command")
                    ?? GetString(payload, "cmd")
                    ?? throw new ArgumentException("payload.command required");
                string? cwd = GetString(payload, "cwd") ?? GetString(payload, "workingDirectory");
                string? desk = GetString(payload, "desktop") ?? GetString(payload, "name");
                if (!string.IsNullOrEmpty(desk))
                {
                    if (!manager.TryGetOwned(desk, out var target) || target == null)
                        throw new InvalidOperationException($"Owned desktop [{desk}] not found");
                    manager.Select(desk);
                }
                var s = RequireSelected(manager);
                int pid = s.LaunchOnDesktop(cmd, cwd);
                return new { ok = true, pid, desktopName = s.DesktopName };
            }
            case "switch":
            case "switchto":
            {
                string? desk = GetString(payload, "desktop") ?? GetString(payload, "name");
                if (string.IsNullOrEmpty(desk))
                    RequireSelected(manager).SwitchTo();
                else
                    manager.SwitchTo(desk);
                return new { ok = true, desktopName = desk ?? manager.Selected?.DesktopName };
            }
            case "switchdefault":
            case "switchback":
                manager.SwitchToDefault();
                return new { ok = true };
            case "close":
            case "closedesktop":
            {
                string desk = GetString(payload, "desktop") ?? GetString(payload, "name")
                    ?? manager.Selected?.DesktopName
                    ?? throw new ArgumentException("payload.desktop required");
                manager.CloseOwned(desk, killProcesses: GetBool(payload, "kill", true));
                return new { ok = true, closed = desk, owned = manager.OwnedNames };
            }
            case "list":
            case "listdesktops":
            {
                var snap = DesktopInventory.Capture();
                return new
                {
                    ok = true,
                    count = snap.Count,
                    desktops = snap.Names,
                    owned = manager.OwnedNames,
                    input = snap.InputDesktopName
                };
            }
            case "capture":
            case "snapshot":
            {
                byte[] png = RequireSelected(manager).CapturePngBytes();
                return new { ok = true, format = "png", base64 = Convert.ToBase64String(png) };
            }
            case "click":
            case "leftclick":
            {
                int x = GetInt(payload, "x", 0);
                int y = GetInt(payload, "y", 0);
                bool stay = GetBool(payload, "stay", false);
                log($"click ({x},{y}) stay={stay}");
                if (stay)
                {
                    if (!manager.IsInteractiveOnVirtualDesktop)
                        manager.SwitchTo(RequireSelected(manager).DesktopName);
                    InputInjector.LeftClick(x, y);
                    return new { ok = true, x, y, mode = "sendinput-stay" };
                }
                string detail = RequireSelected(manager).ClickAt(x, y, rightButton: false);
                return new { ok = true, x, y, mode = "postmessage", detail };
            }
            case "rightclick":
            {
                int x = GetInt(payload, "x", 0);
                int y = GetInt(payload, "y", 0);
                string detail = RequireSelected(manager).ClickAt(x, y, rightButton: true);
                return new { ok = true, x, y, mode = "postmessage", detail };
            }
            case "type":
            case "typetext":
            {
                string text = GetString(payload, "text")
                    ?? throw new ArgumentException("payload.text required");
                string detail = RequireSelected(manager).TypeText(text);
                return new { ok = true, length = text.Length, mode = "postmessage", detail };
            }
            case "status":
            {
                var snap = DesktopInventory.Capture();
                var sel = manager.Selected;
                return new
                {
                    ok = true,
                    created = sel?.IsCreated ?? false,
                    desktopName = sel?.DesktopName,
                    pids = sel?.LaunchedProcessIds,
                    count = snap.Count,
                    owned = manager.OwnedNames
                };
            }
            default:
                throw new ArgumentException($"Unknown action: {action}");
        }
    }

    private static string? GetString(JsonElement payload, string name)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        if (!payload.TryGetProperty(name, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
    }

    private static int GetInt(JsonElement payload, string name, int fallback)
    {
        if (payload.ValueKind != JsonValueKind.Object) return fallback;
        if (!payload.TryGetProperty(name, out var el)) return fallback;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v)) return v;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out v)) return v;
        return fallback;
    }

    private static bool GetBool(JsonElement payload, string name, bool fallback)
    {
        if (payload.ValueKind != JsonValueKind.Object) return fallback;
        if (!payload.TryGetProperty(name, out var el)) return fallback;
        if (el.ValueKind is JsonValueKind.True or JsonValueKind.False) return el.GetBoolean();
        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out bool b)) return b;
        return fallback;
    }
}
