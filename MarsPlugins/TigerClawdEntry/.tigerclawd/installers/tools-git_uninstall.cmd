@echo off
setlocal

echo [Module tools-git] Uninstalling Git (system-wide)...
where git >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo [Module tools-git] Git not on PATH; nothing to uninstall.
  exit /b 0
)
where winget >nul 2>&1
if %ERRORLEVEL% neq 0 (
  echo [Module tools-git] winget not available. Uninstall Git via Windows Settings ^(Apps^).
  exit /b 0
)
winget uninstall --id Git.Git --silent 2>nul
if %ERRORLEVEL% equ 0 (
  echo [Module tools-git] Git uninstalled. Restart terminal if PATH still shows git.
) else (
  echo [Module tools-git] winget uninstall failed or user cancelled. Uninstall via Settings ^(Apps^) if needed.
)
exit /b 0

