# Build LoanIQ Style Demo and create executable with jpackage
# Requires: Java 17+ with jpackage (JDK 14+), Maven

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "=== Maven package ==="
mvn clean package -q
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Ensure app dir has main jar (antrun copies it; dependency-plugin fills app/lib)
$target = "target"
$appDir = Join-Path $target "app"
$mainJar = "LoanIQStyleDemo-1.0.jar"
if (-not (Test-Path (Join-Path $appDir $mainJar))) {
    Copy-Item (Join-Path $target $mainJar) $appDir -Force
}

$jpackageCmd = Get-Command jpackage -ErrorAction SilentlyContinue
if (-not $jpackageCmd) {
    Write-Host "jpackage not found. Install JDK 14+ (with jpackage) or use 'mvn javafx:run' to run the app."
    exit 1
}

$distDir = "dist"
$outName = "LoanIQDemo"
if (Test-Path $distDir) {
    try { Remove-Item -Recurse -Force $distDir -ErrorAction Stop }
    catch { Write-Host "Note: Could not remove $distDir (may be in use). jpackage may overwrite." }
}

Write-Host "=== jpackage (app-image) ==="
# Build main launcher + console launcher (LoanIQDemo-console.exe) to see "Failed to launch JVM" or other errors
$launcherScript = Join-Path $scriptDir "jpackage-launcher-console.properties"
"win-console=true" | Set-Content $launcherScript -Encoding ASCII
jpackage --type app-image `
    --input $appDir `
    --name $outName `
    --main-jar $mainJar `
    --main-class com.mars.javafxdemo.LoanIQStyleDemo `
    --dest $distDir `
    --vendor "Mars" `
    --app-version 1.0 `
    --add-launcher "LoanIQDemo-console=$launcherScript"
Remove-Item $launcherScript -ErrorAction SilentlyContinue

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$outDir = Join-Path $distDir $outName
if (Test-Path $outDir) {
    Write-Host "=== Fix jpackage cfg (merge classpath) ==="
    & (Join-Path $scriptDir "fix-jpackage-cfg.ps1") $outDir
    Write-Host "=== Create Run-console-debug.bat ==="
    & (Join-Path $scriptDir "create-console-debug-bat.ps1") $outDir
}

Write-Host "=== Done. Launcher: $distDir\$outName\LoanIQDemo.exe" -ForegroundColor Green
Write-Host "If exe shows 'Child process exited with code 1', run Run-console-debug.bat in that folder to see the Java error." -ForegroundColor Yellow
