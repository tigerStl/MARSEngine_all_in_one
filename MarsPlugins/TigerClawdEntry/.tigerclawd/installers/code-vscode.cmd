@echo off
setlocal

echo [Module code-vscode] Ensuring VS Code is available for global use (any terminal)...
where code >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module code-vscode] VS Code CLI "code" is on PATH. Use from any terminal: code .
  for /f "delims=" %%i in ('where code 2^>nul') do (for %%j in ("%%i") do echo [Module code-vscode] Install dir: %%~dpj)
  exit /b 0
)
echo [Module code-vscode] "code" not on PATH. Install VS Code and run "Shell Command: Install 'code' command in PATH" for global use.
exit /b 0
