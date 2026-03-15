@echo off
setlocal

echo [FullLocal] Running full validation suite...

if not exist ".tigerclawd\\config\\full-local-config.json" (
  echo [FullLocal][WARN] Full local config not found.
) else (
  echo [FullLocal] Full local config detected.
)

if not exist ".tigerclawd\\config\\full-vectorstore.json" (
  echo [FullLocal][WARN] Full vectorstore config not found.
) else (
  echo [FullLocal] Full vectorstore config detected.
)

if not exist ".tigerclawd\\config\\full-local-permissions.json" (
  echo [FullLocal][WARN] Full local permission profile not found.
) else (
  echo [FullLocal] Full local permission profile detected.
)

echo [FullLocal] Validation suite completed. See warnings above if any.

exit /b 0
