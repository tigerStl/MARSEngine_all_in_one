@echo off
setlocal

echo [BasicCoding] Validating basic coding setup...

if not exist ".tigerclawd\\config\\llm-placeholder.json" (
  echo [BasicCoding][WARN] LLM placeholder config not found.
) else (
  echo [BasicCoding] LLM placeholder config detected.
)

if not exist ".tigerclawd\\state\\code-bridge.txt" (
  echo [BasicCoding][WARN] Code bridge marker not found.
) else (
  echo [BasicCoding] Code bridge marker detected.
)

echo [BasicCoding] Validation completed. See warnings above if any.

exit /b 0
