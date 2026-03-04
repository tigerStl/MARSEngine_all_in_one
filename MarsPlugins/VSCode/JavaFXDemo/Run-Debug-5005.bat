@echo off
cd /d "%~dp0"

if not exist "target\LoanIQStyleDemo-1.0.jar" (
  echo Error: target\LoanIQStyleDemo-1.0.jar not found. Run: mvn clean package -DskipTests
  pause
  exit /b 1
)

echo Starting JavaFXDemo with remote debug on port 5005 (attach anytime to localhost:5005)...
echo.

if defined JAVA_HOME (
  "%JAVA_HOME%\bin\java.exe" -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005 --module-path "target\app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "target\LoanIQStyleDemo-1.0.jar" com.mars.javafxdemo.LoanIQStyleDemo
) else (
  java -agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:5005 --module-path "target\app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "target\LoanIQStyleDemo-1.0.jar" com.mars.javafxdemo.LoanIQStyleDemo
)

pause
