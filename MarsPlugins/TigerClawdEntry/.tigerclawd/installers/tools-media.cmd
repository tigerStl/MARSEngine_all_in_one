@echo off
setlocal

echo [Module tools-media] Checking/installing media tools (global)...
where ffmpeg >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module tools-media] ffmpeg already on PATH.
  for /f "delims=" %%i in ('where ffmpeg 2^>nul') do (for %%j in ("%%i") do echo [Module tools-media] Install dir: %%~dpj)
  exit /b 0
)
echo [Module tools-media] ffmpeg not found. Install globally for use outside editor, e.g.:
echo   winget install ffmpeg
echo   or choco install ffmpeg
echo [Module tools-media] Skipping install; add ffmpeg to PATH for global use.
echo [Module tools-media] After install, typical dir: %ProgramFiles%\ffmpeg or check PATH.
exit /b 0
