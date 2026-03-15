@echo off
setlocal

echo [AgentSetup] Initializing agent runtime...

if not exist ".tigerclawd\\state" (
  mkdir ".tigerclawd\\state"
)
> ".tigerclawd\\state\\agent-runtime.txt" echo Agent runtime initialized at %CD%

echo [AgentSetup] Agent runtime initialization completed.

exit /b 0
