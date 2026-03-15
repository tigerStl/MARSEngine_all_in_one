@echo off
setlocal
where python >nul 2>&1 || (echo [Module llm-gemini] Python not found; nothing to uninstall. & exit /b 0)
echo [Module llm-gemini] Uninstalling Gemini LLM client...
python -m pip uninstall -y google-generativeai 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y google-generativeai 2>nul
echo [Module llm-gemini] Uninstall complete.
exit /b 0
