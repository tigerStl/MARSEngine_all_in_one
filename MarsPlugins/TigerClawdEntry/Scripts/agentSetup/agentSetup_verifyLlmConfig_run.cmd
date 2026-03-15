@echo off
setlocal

echo [AgentSetup] Verifying LLM configuration...

if exist ".tigerclawd\\config\\llm-placeholder.json" (
  echo [AgentSetup] Found LLM config placeholder at .tigerclawd\config\llm-placeholder.json
) else (
  echo [AgentSetup][WARN] No LLM config detected. Agent setup will be limited until a provider is configured.
)

exit /b 0
