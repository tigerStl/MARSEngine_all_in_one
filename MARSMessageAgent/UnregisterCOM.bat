@echo off
setlocal
set EXE=%~dp0MARSMessageAgent\bin\Release\MARSMessageAgent.exe
if not exist "%EXE%" set EXE=%~dp0MARSMessageAgent\bin\Debug\MARSMessageAgent.exe
if not exist "%EXE%" (
    echo MARSMessageAgent.exe not found.
    exit /b 1
)
set REGASM=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
if not exist "%REGASM%" set REGASM=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
echo Unregistering: %EXE%
"%REGASM%" "%EXE%" /unregister
echo Done.
endlocal
