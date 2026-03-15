@echo off
setlocal

echo [AgentSetup] Environment scan starting...
echo [AgentSetup] Workspace: %CD%
where node >nul 2>&1 && echo [AgentSetup] Detected Node.js || echo [AgentSetup][WARN] Node.js not found
where git >nul 2>&1 && echo [AgentSetup] Detected Git || echo [AgentSetup][WARN] Git not found
echo [AgentSetup] Environment scan completed.

exit /b 0
