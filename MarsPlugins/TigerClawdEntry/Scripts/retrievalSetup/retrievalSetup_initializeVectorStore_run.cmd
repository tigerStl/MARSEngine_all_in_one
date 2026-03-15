@echo off
setlocal

echo [RetrievalSetup] Initializing vector store configuration...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\vectorstore.json" echo { "mode": "local", "path": ".tigerclawd/vectorstore" }

echo [RetrievalSetup] Vector store config written to .tigerclawd\config\vectorstore.json

exit /b 0
