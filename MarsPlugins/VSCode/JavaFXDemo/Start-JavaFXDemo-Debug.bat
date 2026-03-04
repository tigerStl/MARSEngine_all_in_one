@echo off
cd /d "%~dp0"

if exist "dist\LoanIQDemo\app\LoanIQStyleDemo-1.0.jar" (
  set "DEST=dist\LoanIQDemo"
) else if exist "dist-new\LoanIQDemo\app\LoanIQStyleDemo-1.0.jar" (
  set "DEST=dist-new\LoanIQDemo"
) else (
  echo Error: dist\LoanIQDemo or dist-new\LoanIQDemo not found. Run build-exe.ps1 first.
  pause
  exit /b 1
)

cd /d "%~dp0%DEST%"
echo Starting JavaFXDemo with remote debug on port 5005...
echo Attach your debugger to localhost:5005
echo.

if defined JAVA_HOME (
  "%JAVA_HOME%\bin\java.exe" -agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=*:5005 --module-path "app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "app\LoanIQStyleDemo-1.0.jar" com.mars.javafxdemo.LoanIQStyleDemo
) else (
  java -agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=*:5005 --module-path "app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "app\LoanIQStyleDemo-1.0.jar" com.mars.javafxdemo.LoanIQStyleDemo
)

pause
