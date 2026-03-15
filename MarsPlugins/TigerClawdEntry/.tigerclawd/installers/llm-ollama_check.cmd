@echo off
setlocal
where ollama >nul 2>&1
exit /b %ERRORLEVEL%
