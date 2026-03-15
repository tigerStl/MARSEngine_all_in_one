@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module llm-anthropic-compatible] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module llm-anthropic-compatible] Installing Anthropic-compatible LLM client (global/user)...
python -m pip install --user anthropic 2>nul
if %ERRORLEVEL% neq 0 pip install --user anthropic 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module llm-anthropic-compatible] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module llm-anthropic-compatible] Installed. Use from any terminal: python -c "import anthropic"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module llm-anthropic-compatible] Install dir: %%i\site-packages
exit /b 0
