@echo off
setlocal

echo [Prereq] Checking for Node.js and npm...

where node >nul 2>&1 && where npm >nul 2>&1 && goto :ok

echo [Prereq] Node.js or npm not found on PATH.
echo [Prereq] Attempting to install Node.js LTS via winget...

where winget >nul 2>&1 || goto :no_winget

winget install --id OpenJS.NodeJS.LTS --accept-package-agreements --accept-source-agreements
if %ERRORLEVEL% equ 0 (
  echo [Prereq] Node.js LTS installed (system-wide). Restart the terminal or Cursor so PATH is updated.
  echo [Prereq] Then node and npm are available from any terminal, outside the editor.
  exit /b 0
)

:no_winget
echo [Prereq] winget not available or install failed.
echo [Prereq] Please install Node.js manually: https://nodejs.org
echo [Prereq] Or run: winget install OpenJS.NodeJS.LTS
exit /b 1

:ok
node -v
npm -v
echo [Prereq] Node.js and npm are available (global use from any terminal).
for /f "delims=" %%i in ('npm root -g 2^>nul') do echo [Prereq] Global npm dir: %%i
exit /b 0
