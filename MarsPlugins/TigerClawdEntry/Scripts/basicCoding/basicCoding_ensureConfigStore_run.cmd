@echo off
setlocal

echo [BasicCoding] Ensuring config store...
if not exist ".tigerclawd" (
  mkdir ".tigerclawd"
)
if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
echo [BasicCoding] Config store ready under .tigerclawd\config.

exit /b 0
