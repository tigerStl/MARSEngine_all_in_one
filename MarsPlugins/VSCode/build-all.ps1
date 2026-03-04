# Build all: extension, java agent, C# ProcessInfo
# Run from repo root: .\build-all.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "=== 1/3 Extension (TypeScript) ===" -ForegroundColor Cyan
Push-Location $root
try {
    npm run compile
    if ($LASTEXITCODE -ne 0) { throw "npm run compile failed" }
} finally {
    Pop-Location
}

Write-Host "`n=== 2/3 Java Agent (Maven) ===" -ForegroundColor Cyan
Push-Location (Join-Path $root "java")
try {
    mvn clean package -DskipTests
    if ($LASTEXITCODE -ne 0) { throw "mvn clean package failed" }
} finally {
    Pop-Location
}

Write-Host "`n=== 3/3 C# ProcessInfo (dotnet) ===" -ForegroundColor Cyan
Push-Location (Join-Path $root "ProcessInfo")
try {
    dotnet publish -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
} finally {
    Pop-Location
}

Write-Host "`n=== Build all done ===" -ForegroundColor Green
