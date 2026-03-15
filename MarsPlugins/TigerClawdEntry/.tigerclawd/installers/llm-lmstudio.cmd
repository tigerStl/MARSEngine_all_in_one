@echo off
setlocal

echo [Module llm-lmstudio] LM Studio is a desktop app. Checking for existing install...
where lmstudio >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module llm-lmstudio] LM Studio already on PATH.
  if exist "%LOCALAPPDATA%\LM Studio\LM Studio.exe" echo [Module llm-lmstudio] Install dir: %LOCALAPPDATA%\LM Studio
  if exist "%ProgramFiles%\LM Studio\LM Studio.exe" echo [Module llm-lmstudio] Install dir: %ProgramFiles%\LM Studio
  exit /b 0
)
echo [Module llm-lmstudio] Install LM Studio from https://lmstudio.ai and add to PATH if needed.
echo [Module llm-lmstudio] Typical install dir: %LOCALAPPDATA%\LM Studio
exit /b 0
