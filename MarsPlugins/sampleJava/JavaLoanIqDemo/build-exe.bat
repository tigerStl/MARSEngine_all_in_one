@echo off
setlocal

rem Always run from the directory of this script
cd /d "%~dp0"

set "SRC=src\main\java"
set "OUT=build\classes"
set "JAR=build\loaniq-ui-demo-1.0.0.jar"
set "MAIN=com.demo.loaniq.LoanIQDemoApp"
set "JP_INPUT=build\jpackage-input"
set "JP_DEST=build"
set "JP_TEMP=build\jpackage-temp"

if not exist "build" mkdir "build"
if not exist "%OUT%" mkdir "%OUT%"

echo [1/3] Compiling Java sources...
javac -encoding UTF-8 -d "%OUT%" -sourcepath "%SRC%" "%SRC%\com\demo\loaniq\LoanIQDemoApp.java"
if errorlevel 1 (
  echo Compile failed.
  exit /b 1
)

echo [2/3] Building runnable JAR...
jar cfe "%JAR%" "%MAIN%" -C "%OUT%" .
if errorlevel 1 (
  echo JAR build failed.
  exit /b 1
)

echo [3/3] Creating Windows app image (EXE) with jpackage...
rem This creates build\LoanIQDemo\LoanIQDemo.exe (no installer, just a runnable folder)
if exist "%JP_INPUT%" rmdir /s /q "%JP_INPUT%"
if exist "%JP_TEMP%" rmdir /s /q "%JP_TEMP%"
if exist "build\LoanIQDemo" rmdir /s /q "build\LoanIQDemo"
mkdir "%JP_INPUT%"
copy /y "%JAR%" "%JP_INPUT%\loaniq-ui-demo-1.0.0.jar" >nul

jpackage --type app-image ^
  --input "%JP_INPUT%" ^
  --name LoanIQDemo ^
  --main-jar loaniq-ui-demo-1.0.0.jar ^
  --main-class "%MAIN%" ^
  --app-version 1.0 ^
  --dest "%JP_DEST%" ^
  --temp "%JP_TEMP%"

if errorlevel 1 (
  echo jpackage failed. Make sure you are using JDK 14+ with jpackage available.
  exit /b 1
)

echo.
echo Done.
echo JAR file: build\loaniq-ui-demo-1.0.0.jar
echo EXE file: build\LoanIQDemo\LoanIQDemo.exe
echo You can distribute the whole LoanIQDemo folder as a portable app.

endlocal

