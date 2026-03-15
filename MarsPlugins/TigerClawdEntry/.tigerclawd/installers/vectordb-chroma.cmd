@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module vectordb-chroma] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module vectordb-chroma] Installing Chroma vector DB client (global/user)...
python -m pip install --user chromadb 2>nul
if %ERRORLEVEL% neq 0 pip install --user chromadb 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module vectordb-chroma] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module vectordb-chroma] Installed. Use from any terminal: python -c "import chromadb"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module vectordb-chroma] Install dir: %%i\site-packages
exit /b 0
