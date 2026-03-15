@echo off
setlocal

echo [Module llm-ollama] Checking Ollama (local LLM runner)...
where ollama >nul 2>&1
if %ERRORLEVEL% equ 0 (
  echo [Module llm-ollama] Ollama already on PATH.
  for /f "delims=" %%i in ('where ollama 2^>nul') do (for %%j in ("%%i") do echo [Module llm-ollama] Install dir: %%~dpj)
  exit /b 0
)

echo [Module llm-ollama] Ollama not found. Attempting install via winget...
winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements --silent 2>nul
if %ERRORLEVEL% equ 0 (
  echo [Module llm-ollama] Installed. Restart terminal and run: ollama --help
  echo [Module llm-ollama] Install dir: %LOCALAPPDATA%\Programs\Ollama
  exit /b 0
)

echo [Module llm-ollama] winget install failed or unavailable. Install manually from https://ollama.com
exit /b 0
