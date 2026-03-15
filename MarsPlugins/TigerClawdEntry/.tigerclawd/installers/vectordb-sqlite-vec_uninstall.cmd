@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-sqlite-vec] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-sqlite-vec] Uninstalling sqlite-vec...
python -m pip uninstall -y sqlite-vec 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y sqlite-vec 2>nul
echo [Module vectordb-sqlite-vec] Uninstall complete.
exit /b 0
