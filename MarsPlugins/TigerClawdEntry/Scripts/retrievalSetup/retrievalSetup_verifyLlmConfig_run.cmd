@echo off
setlocal

echo [RetrievalSetup] Verifying LLM config...

if exist ".tigerclawd\\config\\llm-placeholder.json" (
  echo [RetrievalSetup] Found LLM config placeholder.
) else (
  echo [RetrievalSetup][WARN] No LLM config detected. Retrieval answers may be degraded.
)

exit /b 0
