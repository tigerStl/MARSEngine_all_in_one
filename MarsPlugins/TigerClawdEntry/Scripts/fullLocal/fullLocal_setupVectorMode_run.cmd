@echo off
setlocal

echo [FullLocal] Setting up vector mode...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\full-vectorstore.json" echo { "mode": "local", "path": ".tigerclawd/vector-full" }

echo [FullLocal] Vector mode configuration written to .tigerclawd\config\full-vectorstore.json

exit /b 0
