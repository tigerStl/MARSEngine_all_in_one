@echo off
setlocal
where npm >nul 2>&1 || (echo [Module tools-browser] Node/npm not found; nothing to uninstall. & exit /b 0)
echo [Module tools-browser] Uninstalling Playwright...
call npm uninstall -g playwright 2>nul
echo [Module tools-browser] Uninstall complete.
exit /b 0
