@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-weaviate] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-weaviate] Uninstalling Weaviate client...
python -m pip uninstall -y weaviate-client 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y weaviate-client 2>nul
echo [Module vectordb-weaviate] Uninstall complete.
exit /b 0
