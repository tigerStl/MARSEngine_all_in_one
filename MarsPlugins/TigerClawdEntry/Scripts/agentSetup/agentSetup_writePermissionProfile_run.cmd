@echo off
setlocal

echo [AgentSetup] Writing agent permission profile...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\agent-permissions.json" echo { "profile": "safe-defaults", "allowShell": false, "allowGit": true }

echo [AgentSetup] Permission profile written to .tigerclawd\config\agent-permissions.json
echo [AgentSetup][INFO] Shell commands remain disabled by default. You can edit this profile manually if needed.

exit /b 0
