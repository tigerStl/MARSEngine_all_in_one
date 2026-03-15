@echo off
setlocal

echo [ToolingSetup] Writing tools permission profile...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\tools-permissions.json" echo { "profile": "safe", "allowShell": false, "allowGit": true, "allowTests": true }

echo [ToolingSetup] Tools permission profile written to .tigerclawd\config\tools-permissions.json

exit /b 0
