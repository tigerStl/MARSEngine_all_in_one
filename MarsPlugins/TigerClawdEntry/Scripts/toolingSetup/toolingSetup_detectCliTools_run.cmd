@echo off
setlocal

echo [ToolingSetup] Detecting CLI tools...

where git >nul 2>&1 && echo [ToolingSetup] Git available || echo [ToolingSetup][WARN] Git not found
where npm >nul 2>&1 && echo [ToolingSetup] npm available || echo [ToolingSetup][WARN] npm not found
where dotnet >nul 2>&1 && echo [ToolingSetup] dotnet available || echo [ToolingSetup][INFO] dotnet not found

echo [ToolingSetup] CLI tool detection completed.

exit /b 0
