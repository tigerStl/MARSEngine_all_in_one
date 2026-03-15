@echo off
setlocal

echo [AgentSetup] Validating agent setup...

if not exist ".tigerclawd\\state\\agent-runtime.txt" (
  echo [AgentSetup][WARN] Agent runtime marker not found.
) else (
  echo [AgentSetup] Agent runtime marker detected.
)

if not exist ".tigerclawd\\config\\agent-permissions.json" (
  echo [AgentSetup][WARN] Agent permission profile not found.
) else (
  echo [AgentSetup] Agent permission profile detected.
)

echo [AgentSetup] Validation completed. See warnings above if any.

exit /b 0
