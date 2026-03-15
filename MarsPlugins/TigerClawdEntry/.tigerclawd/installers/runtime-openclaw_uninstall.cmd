@echo off
setlocal
where python >nul 2>&1 || (echo [OpenClaw] Python not found; nothing to uninstall. & exit /b 0)
echo [OpenClaw] Uninstalling OpenClaw runtime...
python -m pip uninstall -y openclaw 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y openclaw 2>nul
echo [OpenClaw] Uninstall complete.
exit /b 0

