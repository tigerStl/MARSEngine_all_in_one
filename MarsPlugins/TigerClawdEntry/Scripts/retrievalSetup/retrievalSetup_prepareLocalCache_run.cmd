@echo off
setlocal

echo [RetrievalSetup] Preparing local cache and index folders...

if not exist ".tigerclawd\\vectorstore" (
  mkdir ".tigerclawd\\vectorstore"
)
if not exist ".tigerclawd\\vectorstore\\cache" (
  mkdir ".tigerclawd\\vectorstore\\cache"
)

echo [RetrievalSetup] Local cache and index folders prepared under .tigerclawd\vectorstore

exit /b 0
