# Fix jpackage .cfg for JavaFX app:
# 1) Merge multiple app.classpath into one line (fixes "Child process exited with code 1").
# 2) Use --module-path and --add-modules for JavaFX so the JVM loads JavaFX correctly (fixes "JavaFX runtime components are missing").
# Usage: .\fix-jpackage-cfg.ps1 [destDir]   e.g. .\fix-jpackage-cfg.ps1 dist-new\LoanIQDemo

param([string]$destDir)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $destDir) {
    if (Test-Path (Join-Path $scriptDir "dist\LoanIQDemo")) { $destDir = Join-Path $scriptDir "dist\LoanIQDemo" }
    elseif (Test-Path (Join-Path $scriptDir "dist-new\LoanIQDemo")) { $destDir = Join-Path $scriptDir "dist-new\LoanIQDemo" }
    else { Write-Error "No dist\LoanIQDemo or dist-new\LoanIQDemo. Pass destDir." }
}

$appDir = Join-Path $destDir "app"
$cfgPath = Join-Path $appDir "LoanIQDemo.cfg"
if (-not (Test-Path $cfgPath)) { Write-Host "Config not found: $cfgPath"; exit 0 }

$lines = Get-Content $cfgPath -Encoding UTF8
$classpathEntries = New-Object System.Collections.ArrayList
$mainclass = ""
$javaOptions = New-Object System.Collections.ArrayList

foreach ($line in $lines) {
    if ($line -match '^\s*app\.classpath=(.+)$') { [void]$classpathEntries.Add($Matches[1].Trim()) }
    elseif ($line -match '^\s*app\.mainclass=(.+)$') { $mainclass = $Matches[1].Trim() }
    elseif ($line -match '^\s*java-options=(.+)$') { [void]$javaOptions.Add($Matches[1].Trim()) }
}
if (-not $mainclass) { $mainclass = "com.mars.javafxdemo.LoanIQStyleDemo" }

# Use only main app jar on classpath; load JavaFX via --module-path + --add-modules (fixes "JavaFX runtime components are missing")
$mainJar = "LoanIQStyleDemo-1.0.jar"
$mergedClasspath = ($classpathEntries | Where-Object { $_ -ne '' }) -join ';'
$useJavaFxModulePath = $mergedClasspath -match "javafx"
if ($useJavaFxModulePath) {
    $classpathLine = "app.classpath=`$APPDIR\$mainJar"
} else {
    $classpathLine = "app.classpath=$mergedClasspath"
}

$newContent = @("[Application]", $classpathLine, "app.mainclass=$mainclass", "[JavaOptions]")
# Use relative path "app" so we don't rely on $APPDIR expansion in [JavaOptions] (exe launcher may not expand it; Run-console-debug.bat works because it uses %~dp0app).
if ($useJavaFxModulePath) {
    $newContent += "java-options=--module-path"
    $newContent += "java-options=app"
    $newContent += "java-options=--add-modules javafx.controls,javafx.fxml,javafx.graphics"
}
foreach ($opt in $javaOptions) {
    if ($opt -notmatch '^--module-path' -and $opt -notmatch '^\$APPDIR' -and $opt -notmatch '^--add-modules' -and $opt -notmatch '^javafx\.') {
        $newContent += "java-options=$opt"
    }
}
if ($javaOptions.Count -eq 0) { $newContent += "java-options=-Djpackage.app-version=1.0" }
Set-Content -Path $cfgPath -Value $newContent -Encoding UTF8
Write-Host "Fixed: $cfgPath (classpath + JavaFX module-path)"
