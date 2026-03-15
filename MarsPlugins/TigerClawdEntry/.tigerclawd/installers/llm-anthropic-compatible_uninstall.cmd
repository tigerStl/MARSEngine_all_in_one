@echo off
setlocal
where python >nul 2>&1 || (echo [Module llm-anthropic-compatible] Python not found; nothing to uninstall. & exit /b 0)
echo [Module llm-anthropic-compatible] Uninstalling Anthropic-compatible LLM client...
python -m pip uninstall -y anthropic 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y anthropic 2>nul
echo [Module llm-anthropic-compatible] Uninstall complete.
exit /b 0
