@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module vectordb-sqlite-vec] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module vectordb-sqlite-vec] Installing sqlite-vec (global/user)...
python -m pip install --user sqlite-vec 2>nul
if %ERRORLEVEL% neq 0 pip install --user sqlite-vec 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module vectordb-sqlite-vec] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module vectordb-sqlite-vec] Installed. Use from any terminal: python -c "import sqlite_vec"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module vectordb-sqlite-vec] Install dir: %%i\site-packages
exit /b 0
