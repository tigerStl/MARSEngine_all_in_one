@echo off
setlocal
where python >nul 2>&1 || (
  echo [Module agent-crewai] This module requires Python. Install from https://www.python.org/ and add to PATH.
  exit /b 1
)
echo [Module agent-crewai] Installing CrewAI (global/user)...
python -m pip install --user crewai 2>nul
if %ERRORLEVEL% neq 0 pip install --user crewai 2>nul
if %ERRORLEVEL% neq 0 (
  echo [Module agent-crewai] pip install failed. Install Python from https://www.python.org/
  exit /b 1
)
echo [Module agent-crewai] Installed. Use from any terminal: python -c "import crewai"
for /f "delims=" %%i in ('python -c "import site; print(site.USER_BASE)" 2^>nul') do echo [Module agent-crewai] Install dir: %%i\site-packages
exit /b 0
