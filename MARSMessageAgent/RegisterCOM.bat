@echo off
setlocal
:: 需要管理员权限修改注册表。请右键“以管理员身份运行”。
set EXE=%~dp0MARSMessageAgent\bin\Release\MARSMessageAgent.exe
if not exist "%EXE%" set EXE=%~dp0MARSMessageAgent\bin\Debug\MARSMessageAgent.exe
if not exist "%EXE%" (
    echo MARSMessageAgent.exe not found. Build the project first.
    pause
    exit /b 1
)
set REGASM=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe
if not exist "%REGASM%" set REGASM=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe
echo Registering COM Local Server: %EXE%
"%REGASM%" "%EXE%" /codebase
if errorlevel 1 (
    echo Registration failed. Try running this script as Administrator.
    pause
    exit /b 1
)
echo Done. Use UnregisterCOM.bat to unregister.
pause
endlocal
