@echo off
echo Checking deployment requirements...

echo.
echo 1. Checking .NET Framework version...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" /v Release
if %errorlevel% neq 0 (
    echo ERROR: .NET Framework 4.7.2 or higher not found!
    pause
    exit /b 1
)

echo.
echo 2. Checking required DLLs...
if not exist "MarsInterMQCenter.dll" (
    echo ERROR: Main executable not found!
    pause
    exit /b 1
)

if not exist "MarsInterMQCenter.dll.config" (
    echo ERROR: Configuration file not found!
    pause
    exit /b 1
)

echo.
echo 3. Checking log directory...
if not exist "log" mkdir log

echo.
echo 4. Checking COM registration...
reg query "HKEY_CLASSES_ROOT\Accessibility.IAccessible" >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Accessibility COM components may not be registered
)

echo.
echo 5. Testing application startup...
echo Starting application...
MarsInterMQCenter.exe
if %errorlevel% neq 0 (
    echo ERROR: Application failed to start with error code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Deployment check completed successfully!
pause
