@echo off
setlocal

echo [FullLocal] Writing full local permission profile...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\full-local-permissions.json" echo { "profile": "balanced", "allowShell": true, "allowGit": true, "allowTests": true }

echo [FullLocal] Full local permission profile written to .tigerclawd\config\full-local-permissions.json

exit /b 0
