@echo off
setlocal
where python >nul 2>&1 || (echo [Module tools-database] Python not found; nothing to uninstall. & exit /b 0)
echo [Module tools-database] Uninstalling database client...
python -m pip uninstall -y sqlalchemy 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y sqlalchemy 2>nul
echo [Module tools-database] Uninstall complete.
exit /b 0
