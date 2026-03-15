@echo off
setlocal
where lmstudio >nul 2>&1
if %ERRORLEVEL% equ 0 exit /b 0
if exist "%LOCALAPPDATA%\LM Studio\LM Studio.exe" exit /b 0
if exist "%ProgramFiles%\LM Studio\LM Studio.exe" exit /b 0
exit /b 1
