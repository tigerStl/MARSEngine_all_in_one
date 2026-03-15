@echo off
setlocal

echo [BasicCoding] Bootstrapping LLM config...
if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
if not exist ".tigerclawd\\config\\llm-placeholder.json" (
  > ".tigerclawd\\config\\llm-placeholder.json" echo { "provider": "placeholder", "status": "not-configured" }
  echo [BasicCoding] Created LLM placeholder config at .tigerclawd\config\llm-placeholder.json
) else (
  echo [BasicCoding] LLM config already exists.
)

exit /b 0
