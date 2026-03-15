@echo off
setlocal

echo [AgentSetup] Verifying code bridge...

if exist ".tigerclawd\\state\\code-bridge.txt" (
  echo [AgentSetup] Code bridge marker detected.
) else (
  echo [AgentSetup][WARN] Code bridge marker not found. Agents may not see the current workspace correctly.
)

exit /b 0
