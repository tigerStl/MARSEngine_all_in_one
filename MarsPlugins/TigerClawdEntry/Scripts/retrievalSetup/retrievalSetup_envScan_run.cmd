@echo off
setlocal

echo [RetrievalSetup] Environment scan starting...
echo [RetrievalSetup] Workspace: %CD%
where node >nul 2>&1 && echo [RetrievalSetup] Detected Node.js || echo [RetrievalSetup][WARN] Node.js not found
echo [RetrievalSetup] Environment scan completed.

exit /b 0
