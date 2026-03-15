@echo off
setlocal
where python >nul 2>&1 || (echo [Module llm-deepseek-compatible] Python not found; nothing to uninstall. & exit /b 0)
echo [Module llm-deepseek-compatible] Uninstalling OpenAI client (shared with other LLM modules)...
python -m pip uninstall -y openai 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y openai 2>nul
echo [Module llm-deepseek-compatible] Uninstall complete.
exit /b 0
