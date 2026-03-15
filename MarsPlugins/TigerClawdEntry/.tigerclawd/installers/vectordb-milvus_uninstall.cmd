@echo off
setlocal
where python >nul 2>&1 || (echo [Module vectordb-milvus] Python not found; nothing to uninstall. & exit /b 0)
echo [Module vectordb-milvus] Uninstalling Milvus vector DB client...
python -m pip uninstall -y pymilvus 2>nul
if %ERRORLEVEL% neq 0 pip uninstall -y pymilvus 2>nul
echo [Module vectordb-milvus] Uninstall complete.
exit /b 0
