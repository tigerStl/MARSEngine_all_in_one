@echo off
setlocal
where python >nul 2>&1 || (echo [Module agent-crewai] Python not found; nothing to uninstall. & exit /b 0)
echo [Module agent-crewai] Uninstalling CrewAI...
python -m pip uninstall -y crewai 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y crewai 2>nul
echo [Module agent-crewai] Uninstall complete.
exit /b 0
