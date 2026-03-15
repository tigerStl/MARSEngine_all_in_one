@echo off
setlocal

echo [BasicCoding] Initializing code bridge...
echo [BasicCoding] This step would register the current workspace with TigerClawdEntry runtime.
echo [BasicCoding] For now, it records a simple marker file.

if not exist ".tigerclawd\\state" (
  mkdir ".tigerclawd\\state"
)
> ".tigerclawd\\state\\code-bridge.txt" echo Workspace linked at %CD%

echo [BasicCoding] Code bridge initialization completed.

exit /b 0
