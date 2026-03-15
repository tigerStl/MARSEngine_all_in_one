@echo off
setlocal
where python >nul 2>&1 || (echo [Module agent-langgraph] Python not found; nothing to uninstall. & exit /b 0)
echo [Module agent-langgraph] Uninstalling LangGraph agent runtime...
python -m pip uninstall -y langgraph 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y langgraph 2>nul
echo [Module agent-langgraph] Uninstall complete.
exit /b 0
