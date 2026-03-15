@echo off
setlocal

echo [BasicCoding] Enabling basic tools profile...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\tools-basic.json" echo { "profile": "basic", "commands": ["file", "navigation"] }

echo [BasicCoding] Basic tools profile written to .tigerclawd\config\tools-basic.json

exit /b 0
