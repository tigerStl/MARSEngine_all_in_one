# Mars.VirtualDesktop

Mars subsystem EXE (**Core** / `net8.0-windows` WinForms) for testing **Win32 virtual desktops**:

1. Create a virtual desktop (`CreateDesktopW`)
2. Launch a process onto that desktop (`STARTUPINFO.lpDesktop`)
3. Mirror (capture + display) the desktop content in this UI
4. Inject mouse / keyboard (`SendInput`) into the virtual desktop

> This uses classic Win32 **session desktops**, not Windows 10/11 Task View “Virtual Desktops”. Session desktops are better suited for launch / BitBlt capture / SwitchDesktop + input testing.

## Build

```powershell
dotnet build Mars.VirtualDesktop.csproj -c Debug
dotnet run --project Mars.VirtualDesktop.csproj
```

## Run

```powershell
# UI only
.\bin\Debug\net8.0-windows\Mars.VirtualDesktop.exe

# UI + HTTP control
.\bin\Debug\net8.0-windows\Mars.VirtualDesktop.exe --port 17890

# Auto create + launch notepad + HTTP
.\bin\Debug\net8.0-windows\Mars.VirtualDesktop.exe --create --launch "notepad.exe" --port 17890
```

### UI flow

1. **Create Desktop**
2. **Launch App** (default `notepad.exe`)
3. **Start Mirror** — preview updates every 500ms
4. Click the preview to inject left/right click (temporarily switches interactive desktop, injects, switches back)
5. Use **Type** to send Unicode text the same way

## HTTP control (`--port`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Ready probe |
| GET | `/status` | Desktop + PIDs |
| POST | `/invoke` | JSON command |

Example:

```json
POST http://127.0.0.1:17890/invoke
{ "action": "create", "payload": { "name": "MarsVD_Demo" } }

{ "action": "launch", "payload": { "command": "notepad.exe" } }

{ "action": "capture" }

{ "action": "click", "payload": { "x": 200, "y": 200 } }

{ "action": "type", "payload": { "text": "hello from Mars" } }

{ "action": "switch" }
{ "action": "switchback" }
```

## Layout

```
Mars.VirtualDesktop/
  Mars.VirtualDesktop.csproj
  Program.cs
  Desktop/VirtualDesktopSession.cs
  Native/Win32DesktopApi.cs
  Native/InputInjector.cs
  Host/ControlServer.cs
  UI/MainForm.cs
```
