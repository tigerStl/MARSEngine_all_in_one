@echo off
setlocal

echo [Module tools-git] Ensuring Git is available for global use (any terminal)...
where git >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module tools-git] Git is on PATH. Use from any terminal: git status, git clone, etc.
  for /f "delims=" %%i in ('where git 2^>nul') do (for %%j in ("%%i") do echo [Module tools-git] Install dir: %%~dpj)
  exit /b 0
)
echo [Module tools-git] Git not found. Installing for system-wide use via winget...
where winget >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo [Module tools-git] winget not available. Install Git manually: https://git-scm.com
  exit /b 1
)
winget install --id Git.Git --accept-package-agreements --accept-source-agreements
if %ERRORLEVEL% equ 0 (
  echo [Module tools-git] Git installed. Restart terminal for PATH; then "git" works from any shell.
  echo [Module tools-git] Install dir: %ProgramFiles%\Git
  exit /b 0
)
echo [Module tools-git] Install failed. Install manually: https://git-scm.com
exit /b 1
