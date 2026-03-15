@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-faiss] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-faiss] Uninstalling FAISS...
python -m pip uninstall -y faiss-cpu 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y faiss-cpu 2>nul
echo [Module vectordb-faiss] Uninstall complete.
exit /b 0
