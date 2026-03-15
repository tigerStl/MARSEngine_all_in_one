@echo off
setlocal
where python >nul 2>&1 || exit /b 1
python -c "import openai" 2>nul
exit /b %ERRORLEVEL%
