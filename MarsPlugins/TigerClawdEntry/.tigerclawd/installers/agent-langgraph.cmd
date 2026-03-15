@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module agent-langgraph] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module agent-langgraph] Installing LangGraph agent runtime (global/user)...
python -m pip install --user langgraph 2>nul
if %ERRORLEVEL% neq 0 pip install --user langgraph 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module agent-langgraph] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module agent-langgraph] Installed. Use from any terminal: python -c "import langgraph"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module agent-langgraph] Install dir: %%i\site-packages
exit /b 0
