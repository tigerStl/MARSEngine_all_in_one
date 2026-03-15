@echo off
setlocal
where python >nul 2>&1 || (echo [Module llm-openai-compatible] Python not found; nothing to uninstall. & exit /b 0)
echo [Module llm-openai-compatible] Uninstalling OpenAI-compatible LLM client...
python -m pip uninstall -y openai 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y openai 2>nul
echo [Module llm-openai-compatible] Uninstall complete.
exit /b 0
