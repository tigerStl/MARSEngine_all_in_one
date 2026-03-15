@echo off
setlocal
where python >nul 2>&1 || (echo [Module agent-langchain] Python not found; nothing to uninstall. & exit /b 0)
echo [Module agent-langchain] Uninstalling LangChain agent runtime...
python -m pip uninstall -y langchain langchain-openai langchain-anthropic 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y langchain langchain-openai langchain-anthropic 2>nul
echo [Module agent-langchain] Uninstall complete.
exit /b 0
