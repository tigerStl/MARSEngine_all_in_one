@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module vectordb-milvus] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module vectordb-milvus] Installing Milvus vector DB client (global/user)...
python -m pip install --user pymilvus 2>nul
if %ERRORLEVEL% neq 0 pip install --user pymilvus 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module vectordb-milvus] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module vectordb-milvus] Installed. Use from any terminal: python -c "import pymilvus"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module vectordb-milvus] Install dir: %%i\site-packages
exit /b 0
