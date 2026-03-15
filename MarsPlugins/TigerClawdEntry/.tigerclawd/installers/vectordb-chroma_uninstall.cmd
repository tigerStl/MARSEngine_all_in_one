@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-chroma] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-chroma] Uninstalling Chroma vector DB client...
python -m pip uninstall -y chromadb 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y chromadb 2>nul
echo [Module vectordb-chroma] Uninstall complete.
exit /b 0
