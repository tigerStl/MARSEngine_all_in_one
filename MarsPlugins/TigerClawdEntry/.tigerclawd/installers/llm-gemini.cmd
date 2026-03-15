@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module llm-gemini] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module llm-gemini] Installing Gemini LLM client (global/user)...
python -m pip install --user google-generativeai 2>nul
if %ERRORLEVEL% neq 0 pip install --user google-generativeai 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module llm-gemini] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module llm-gemini] Installed. Use from any terminal: python -c "import google.generativeai"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module llm-gemini] Install dir: %%i\site-packages
exit /b 0
