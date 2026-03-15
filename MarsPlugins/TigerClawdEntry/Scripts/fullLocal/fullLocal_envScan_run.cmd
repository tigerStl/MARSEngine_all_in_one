@echo off
setlocal

echo [FullLocal] Full environment scan starting...
echo [FullLocal] Workspace: %CD%
where node >nul 2>&1 && echo [FullLocal] Detected Node.js || echo [FullLocal][WARN] Node.js not found
where git >nul 2>&1 && echo [FullLocal] Detected Git || echo [FullLocal][WARN] Git not found
where python >nul 2>&1 && echo [FullLocal] Detected Python || echo [FullLocal][INFO] Python not found
echo [FullLocal] Full environment scan completed.

exit /b 0
