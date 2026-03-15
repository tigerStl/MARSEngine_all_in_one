@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module vectordb-faiss] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module vectordb-faiss] Installing FAISS (global/user)...
python -m pip install --user faiss-cpu 2>nul
if %ERRORLEVEL% neq 0 pip install --user faiss-cpu 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module vectordb-faiss] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module vectordb-faiss] Installed. Use from any terminal: python -c "import faiss"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module vectordb-faiss] Install dir: %%i\site-packages
exit /b 0
