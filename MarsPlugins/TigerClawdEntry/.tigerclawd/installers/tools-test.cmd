@echo off
setlocal
where npm >nul 2>&1 || (
  echo [Module tools-test] This module requires Node.js and npm. Install from https://nodejs.org/ and add to PATH.
  exit /b 1
)
echo [Module tools-test] Installing test runner CLI (global)... 
call npm install -g jest 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module tools-test] npm install -g failed. Ensure Node and npm are on PATH.
  exit /b 1
)
echo [Module tools-test] Installed. Use from any terminal: jest --version
for /f "delims=" %%i in ('npm root -g 2^>nul') do echo [Module tools-test] Install dir: %%i
exit /b 0
