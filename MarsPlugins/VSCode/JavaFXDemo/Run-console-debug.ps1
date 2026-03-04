# Run LoanIQDemo with system Java (module-path + JavaFX + JDWP on port 5005).
# Optionally generate Run-console-debug.bat in the same directory.
# Usage: .\Run-console-debug.ps1 [destDir] [-GenerateBat]
#   destDir: e.g. dist\LoanIQDemo (default: auto-detect dist\LoanIQDemo or dist-new\LoanIQDemo)

param([string]$destDir, [switch]$GenerateBat)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $destDir) {
    if (Test-Path (Join-Path $scriptDir "dist\LoanIQDemo")) { $destDir = Join-Path $scriptDir "dist\LoanIQDemo" }
    elseif (Test-Path (Join-Path $scriptDir "dist-new\LoanIQDemo")) { $destDir = Join-Path $scriptDir "dist-new\LoanIQDemo" }
    else { Write-Error "No dist\LoanIQDemo or dist-new\LoanIQDemo found. Run build-exe.ps1 first or pass destDir." }
}

$appDir = Join-Path $destDir "app"
$mainJar = "LoanIQStyleDemo-1.0.jar"
if (-not (Test-Path (Join-Path $appDir $mainJar))) {
    Write-Error "Main jar not found: $appDir\$mainJar"
}

if ($GenerateBat) {
    $batPath = Join-Path $destDir "Run-console-debug.bat"
    $bat = @"
@echo off
cd /d "%~dp0"
echo Running with system Java (module-path + JavaFX + JDWP debug on port 5005)...
if defined JAVA_HOME (
  "%JAVA_HOME%\bin\java.exe" -agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=*:5005 --module-path "%~dp0app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "%~dp0app\$mainJar" com.mars.javafxdemo.LoanIQStyleDemo
) else (
  java -agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=*:5005 --module-path "%~dp0app" --add-modules javafx.controls,javafx.fxml,javafx.graphics -cp "%~dp0app\$mainJar" com.mars.javafxdemo.LoanIQStyleDemo
)
pause
"@
    Set-Content -Path $batPath -Value $bat -Encoding ASCII
    Write-Host "Created: $batPath"
    return
}

Write-Host "Running with system Java (module-path + JavaFX + JDWP on port 5005)..."
$javaExe = if ($env:JAVA_HOME) { Join-Path $env:JAVA_HOME "bin\java.exe" } else { "java" }
$jvmArgs = @(
    "-agentlib:jdwp=transport=dt_socket,server=y,suspend=y,address=*:5005",
    "--module-path", "app",
    "--add-modules", "javafx.controls,javafx.fxml,javafx.graphics",
    "-cp", "app\$mainJar",
    "com.mars.javafxdemo.LoanIQStyleDemo"
)
Push-Location $destDir
try {
    & $javaExe @jvmArgs
} finally {
    Pop-Location
}
Write-Host "Press Enter to close..."
Read-Host
