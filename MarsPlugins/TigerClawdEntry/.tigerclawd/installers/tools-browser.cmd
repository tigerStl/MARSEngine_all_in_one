@echo off
setlocal
where npm >nul 2>&1 || (
  echo [Module tools-browser] This module requires Node.js and npm. Install from https://nodejs.org/ and add to PATH.
  exit /b 1
)
echo [Module tools-browser] Installing Playwright (global CLI and browsers)...
call npm install -g playwright 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module tools-browser] npm install -g failed. Ensure Node and npm are on PATH.
  exit /b 1
)
echo [Module tools-browser] Installing browser binaries (run from any terminal): npx playwright install
call npx playwright install 2>nul
echo [Module tools-browser] Installed. Use from any terminal: npx playwright --version
for /f "delims=" %%i in ('npm root -g 2^>nul') do echo [Module tools-browser] Install dir: %%i
exit /b 0
