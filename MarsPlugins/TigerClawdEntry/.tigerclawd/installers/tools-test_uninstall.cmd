@echo off
setlocal
where npm >nul 2>&1 || (echo [Module tools-test] Node/npm not found; nothing to uninstall. & exit /b 0)
echo [Module tools-test] Uninstalling test runner CLI...
call npm uninstall -g jest 2>nul
echo [Module tools-test] Uninstall complete.
exit /b 0
