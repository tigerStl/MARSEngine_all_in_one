@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module agent-langchain] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module agent-langchain] Installing LangChain agent runtime (global/user)...
python -m pip install --user langchain langchain-openai langchain-anthropic 2>nul
if %ERRORLEVEL% neq 0 pip install --user langchain langchain-openai langchain-anthropic 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module agent-langchain] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module agent-langchain] Installed. Use from any terminal: python -c "import langchain"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module agent-langchain] Install dir: %%i\site-packages
exit /b 0
