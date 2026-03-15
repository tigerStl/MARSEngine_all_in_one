@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-pgvector] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-pgvector] Uninstalling pgvector...
python -m pip uninstall -y pgvector 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y pgvector 2>nul
echo [Module vectordb-pgvector] Uninstall complete.
exit /b 0
