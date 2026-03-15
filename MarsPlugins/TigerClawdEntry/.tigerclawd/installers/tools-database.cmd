@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module tools-database] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module tools-database] Installing database client (global/user)...
python -m pip install --user sqlalchemy 2>nul
if %ERRORLEVEL% neq 0 pip install --user sqlalchemy 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module tools-database] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module tools-database] Installed. Use from any terminal: python -c "import sqlalchemy"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module tools-database] Install dir: %%i\site-packages
exit /b 0
