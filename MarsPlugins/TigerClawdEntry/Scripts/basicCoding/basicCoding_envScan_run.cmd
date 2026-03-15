@echo off
setlocal

echo [BasicCoding] Environment scan starting...
echo [BasicCoding] Workspace: %CD%
where node >nul 2>&1 && echo [BasicCoding] Detected Node.js || echo [BasicCoding] Node.js not found
where git >nul 2>&1 && echo [BasicCoding] Detected Git || echo [BasicCoding] Git not found
echo [BasicCoding] Environment scan completed.

exit /b 0
