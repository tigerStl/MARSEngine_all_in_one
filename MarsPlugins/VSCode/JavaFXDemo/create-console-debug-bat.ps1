# Generate Run-console-debug.bat in jpackage output (for build-exe.ps1).
# Delegates to Run-console-debug.ps1 -GenerateBat.
# Usage: .\create-console-debug-bat.ps1 [destDir]

param([string]$destDir)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $scriptDir "Run-console-debug.ps1") -destDir $destDir -GenerateBat
