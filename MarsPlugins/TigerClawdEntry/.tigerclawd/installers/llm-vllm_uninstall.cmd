@echo off
setlocal
where python >nul 2>&1 || (echo [Module llm-vllm] Python not found; nothing to uninstall. & exit /b 0)
echo [Module llm-vllm] Uninstalling vLLM...
python -m pip uninstall -y vllm 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y vllm 2>nul
echo [Module llm-vllm] Uninstall complete.
exit /b 0
