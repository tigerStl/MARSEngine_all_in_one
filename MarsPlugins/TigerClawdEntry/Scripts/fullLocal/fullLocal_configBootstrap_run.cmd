@echo off
setlocal

echo [FullLocal] Bootstrapping global configuration...

if not exist ".tigerclawd" (
  mkdir ".tigerclawd"
)
if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\full-local-config.json" echo { "profile": "full-local", "createdAt": "%DATE% %TIME%" }

echo [FullLocal] Global configuration bootstrapped.

exit /b 0
