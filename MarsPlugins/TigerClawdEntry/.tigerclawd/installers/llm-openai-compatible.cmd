@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module llm-openai-compatible] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module llm-openai-compatible] Installing OpenAI-compatible LLM client (global/user)...
python -m pip install --user openai 2>nul
if %ERRORLEVEL% neq 0 (
  pip install --user openai 2>nul
)
if %ERRORLEVEL% neq 0 (
  echo [Module llm-openai-compatible] pip install failed. Install Python from https://www.python.org/ and ensure pip is available.
  exit /b 1
)
echo [Module llm-openai-compatible] Installed. Use from any terminal: python -c "import openai"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module llm-openai-compatible] Install dir: %%i\site-packages
exit /b 0
