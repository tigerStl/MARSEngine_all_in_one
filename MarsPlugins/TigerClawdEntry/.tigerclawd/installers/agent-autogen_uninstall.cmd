@echo off
setlocal
where python >nul 2>&1 || (echo [Module agent-autogen] Python not found; nothing to uninstall. & exit /b 0)
echo [Module agent-autogen] Uninstalling AutoGen...
python -m pip uninstall -y pyautogen 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y pyautogen 2>nul
echo [Module agent-autogen] Uninstall complete.
exit /b 0
