@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "MVN_CMD="

if defined MAVEN_HOME if exist "%MAVEN_HOME%\bin\mvn.cmd" set "MVN_CMD=%MAVEN_HOME%\bin\mvn.cmd"
if not defined MVN_CMD if exist "%SCRIPT_DIR%.tools\apache-maven-3.9.9\bin\mvn.cmd" set "MVN_CMD=%SCRIPT_DIR%.tools\apache-maven-3.9.9\bin\mvn.cmd"
if not defined MVN_CMD if exist "%SCRIPT_DIR%MarsJavaDemo\.tools\apache-maven-3.9.9\bin\mvn.cmd" set "MVN_CMD=%SCRIPT_DIR%MarsJavaDemo\.tools\apache-maven-3.9.9\bin\mvn.cmd"

if not defined MVN_CMD (
  echo Maven is not on PATH. Use this wrapper, or install Maven and reopen the terminal.
  echo   .\mvnw.cmd -q -DskipTests package
  echo Portable Maven expected at:
  echo   %SCRIPT_DIR%.tools\apache-maven-3.9.9\bin\mvn.cmd
  exit /b 1
)

call "%MVN_CMD%" %*
exit /b %ERRORLEVEL%
