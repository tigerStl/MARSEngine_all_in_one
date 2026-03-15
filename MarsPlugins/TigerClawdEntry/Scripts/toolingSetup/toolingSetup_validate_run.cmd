@echo off
setlocal

echo [ToolingSetup] Validating tooling setup...

if not exist ".tigerclawd\\config\\tools-registry.json" (
  echo [ToolingSetup][WARN] Tools registry not found.
) else (
  echo [ToolingSetup] Tools registry detected.
)

if not exist ".tigerclawd\\config\\tools-permissions.json" (
  echo [ToolingSetup][WARN] Tools permission profile not found.
) else (
  echo [ToolingSetup] Tools permission profile detected.
)

echo [ToolingSetup] Validation completed. See warnings above if any.

exit /b 0
