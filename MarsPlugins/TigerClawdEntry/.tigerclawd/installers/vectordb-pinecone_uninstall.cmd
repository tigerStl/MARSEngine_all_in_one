@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-pinecone] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-pinecone] Uninstalling Pinecone client...
python -m pip uninstall -y pinecone-client 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y pinecone-client 2>nul
echo [Module vectordb-pinecone] Uninstall complete.
exit /b 0
