@echo off
setlocal

echo [ToolingSetup] Environment scan starting...
echo [ToolingSetup] Workspace: %CD%
where git >nul 2>&1 && echo [ToolingSetup] Detected Git || echo [ToolingSetup][WARN] Git not found
where cmd >nul 2>&1 && echo [ToolingSetup] Detected shell || echo [ToolingSetup][WARN] Shell not detected
echo [ToolingSetup] Environment scan completed.

exit /b 0
