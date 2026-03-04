# gradle-wrapper.jar is required for gradlew.bat. It is not published as a standalone file.
# Option 1: Install Gradle (https://gradle.org/install), then run:
#   gradle wrapper --gradle-version 8.5
# Option 2: Copy gradle/wrapper/gradle-wrapper.jar from any Gradle project that uses 8.x.
$outJar = Join-Path $PSScriptRoot "gradle\wrapper\gradle-wrapper.jar"
if (Test-Path $outJar) { Write-Host "Already exists: $outJar"; exit 0 }
Write-Host "gradle-wrapper.jar not found at: $outJar"
Write-Host ""
Write-Host "To create it:"
Write-Host "  1. Install Gradle from https://gradle.org/install"
Write-Host "  2. In this directory run: gradle wrapper --gradle-version 8.5"
Write-Host "Or copy gradle/wrapper/gradle-wrapper.jar from another Gradle 8.x project."
exit 1
