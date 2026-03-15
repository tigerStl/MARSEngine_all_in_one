@echo off
setlocal

echo [Module code-cursor] Ensuring Cursor is available for global use (any terminal)...
where cursor >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module code-cursor] Cursor CLI "cursor" is on PATH. Use from any terminal: cursor .
  for /f "delims=" %%i in ('where cursor 2^>nul') do (for %%j in ("%%i") do echo [Module code-cursor] Install dir: %%~dpj)
  exit /b 0
)
echo [Module code-cursor] "cursor" not on PATH. Add Cursor install dir to PATH for global use, or use Cursor from Start/shortcut.
exit /b 0
