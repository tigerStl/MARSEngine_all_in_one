@echo off
setlocal

echo [Module llm-lmstudio] Uninstalling LM Studio...
set "UNINSTALL_EXE="
set "LM_DIR=%LOCALAPPDATA%\LM Studio"
if exist "%LM_DIR%\LM Studio.exe" (
  if exist "%LM_DIR%\Uninstall LM Studio.exe" (
    set "UNINSTALL_EXE=%LM_DIR%\Uninstall LM Studio.exe"
    goto :run
  )
  if exist "%LM_DIR%\Uninstall.exe" (
    set "UNINSTALL_EXE=%LM_DIR%\Uninstall.exe"
    goto :run
  )
  if exist "%LM_DIR%\unins000.exe" (
    set "UNINSTALL_EXE=%LM_DIR%\unins000.exe"
    goto :run
  )
  for %%u in ("%LM_DIR%\Uninstall*.exe") do (
    set "UNINSTALL_EXE=%%u"
    goto :run
  )
  for %%u in ("%LM_DIR%\unins*.exe") do (
    set "UNINSTALL_EXE=%%u"
    goto :run
  )
)
set "LM_DIR=%ProgramFiles%\LM Studio"
if exist "%LM_DIR%\LM Studio.exe" (
  if exist "%LM_DIR%\Uninstall LM Studio.exe" (
    set "UNINSTALL_EXE=%LM_DIR%\Uninstall LM Studio.exe"
    goto :run
  )
  if exist "%LM_DIR%\unins000.exe" (
    set "UNINSTALL_EXE=%LM_DIR%\unins000.exe"
    goto :run
  )
  for %%u in ("%LM_DIR%\Uninstall*.exe") do (
    set "UNINSTALL_EXE=%%u"
    goto :run
  )
  for %%u in ("%LM_DIR%\unins*.exe") do (
    set "UNINSTALL_EXE=%%u"
    goto :run
  )
)

echo [Module llm-lmstudio] No uninstaller found in app dir; trying winget...
winget uninstall "LM Studio.LM Studio" --silent 2>nul
if %ERRORLEVEL% equ 0 goto :done
winget uninstall "LM-Studio.LM-Studio" --silent 2>nul
goto :done

:run
echo [Module llm-lmstudio] Running: "%UNINSTALL_EXE%"
start "" /wait "%UNINSTALL_EXE%"
:done
exit /b 0
