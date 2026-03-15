@echo off
setlocal

echo [ToolingSetup] Initializing tool registry...

if not exist ".tigerclawd\\config" (
  mkdir ".tigerclawd\\config"
)
> ".tigerclawd\\config\\tools-registry.json" echo { "registeredTools": ["shell", "file", "git"], "status": "initialized" }

echo [ToolingSetup] Tool registry written to .tigerclawd\config\tools-registry.json

exit /b 0
