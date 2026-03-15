@echo off
setlocal

echo [RetrievalSetup] Validating retrieval setup...

if not exist ".tigerclawd\\config\\vectorstore.json" (
  echo [RetrievalSetup][WARN] Vector store config not found.
) else (
  echo [RetrievalSetup] Vector store config detected.
)

if not exist ".tigerclawd\\vectorstore\\cache" (
  echo [RetrievalSetup][WARN] Cache folder not found.
) else (
  echo [RetrievalSetup] Cache folder detected.
)

echo [RetrievalSetup] Validation completed. See warnings above if any.

exit /b 0
