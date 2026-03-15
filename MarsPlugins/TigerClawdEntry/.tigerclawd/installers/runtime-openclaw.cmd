@echo off
setlocal
where python >nul 2>&1 || (
  echo [OpenClaw] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [OpenClaw] Installing OpenClaw runtime for Basic Coding (global/user)...
python -m pip install --user openclaw 2>nul
if %ERRORLEVEL% neq 0 pip install --user openclaw 2>nul
if %ERRORLEVEL% neq 0 (
  echo [OpenClaw] pip install openclaw failed or package not found. Install Python from https://www.python.org/
  exit /b 1
)
echo [OpenClaw] Done. Use from any terminal when package is installed.
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [OpenClaw] Install dir: %%i\site-packages
exit /b 0

