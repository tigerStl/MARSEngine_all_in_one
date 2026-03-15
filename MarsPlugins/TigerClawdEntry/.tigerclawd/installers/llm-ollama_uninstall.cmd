@echo off
setlocal

echo [Module llm-ollama] Uninstalling Ollama...
set "UNINSTALL_EXE="
for /f "delims=" %%i in ('where ollama 2^>nul') do (
  set "OLLAMA_EXE=%%i"
  goto :found_ollama
)
goto :no_path

:found_ollama
for %%i in ("%OLLAMA_EXE%") do set "OLLAMA_DIR=%%~dpi"
if not defined OLLAMA_DIR goto :no_path
set "OLLAMA_DIR=%OLLAMA_DIR:~0,-1%"

if exist "%OLLAMA_DIR%\Uninstall Ollama.exe" (
  set "UNINSTALL_EXE=%OLLAMA_DIR%\Uninstall Ollama.exe"
  goto :run_uninstall
)
if exist "%OLLAMA_DIR%\Uninstall.exe" (
  set "UNINSTALL_EXE=%OLLAMA_DIR%\Uninstall.exe"
  goto :run_uninstall
)
if exist "%OLLAMA_DIR%\unins000.exe" (
  set "UNINSTALL_EXE=%OLLAMA_DIR%\unins000.exe"
  goto :run_uninstall
)
for %%u in ("%OLLAMA_DIR%\Uninstall*.exe") do (
  set "UNINSTALL_EXE=%%u"
  goto :run_uninstall
)
for %%u in ("%OLLAMA_DIR%\unins*.exe") do (
  set "UNINSTALL_EXE=%%u"
  goto :run_uninstall
)

:no_path
echo [Module llm-ollama] Ollama not on PATH; trying winget uninstall...
winget uninstall Ollama.Ollama --silent 2>nul
exit /b 0

:run_uninstall
echo [Module llm-ollama] Running: "%UNINSTALL_EXE%"
start "" /wait "%UNINSTALL_EXE%"
exit /b 0
