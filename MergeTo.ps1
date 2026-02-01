param(
    [Parameter(Mandatory = $true)]
    [string]$SourceBranch
)

# 仓库目录数组，直接在脚本里定义
$RepoDirs = @(
    "C:\work\MARS\ManagedInjector",
    "C:\work\MARS\TestFlow",
    "C:\work\MARS\Mars.Inter.MQCenter",
    "C:\work\MARS\Mars.LogCommon",
    "C:\work\MARS\Mars.plugins.standards",
    "C:\work\MARS\Mars.Securities",
    "C:\work\MARS\MarsErrorMessageBox",
    "C:\work\MARS\MarsDllInjectorHost",
    "C:\work\MARS\ShellBasic",
    "C:\work\MARS\MARSWebDriver",
    "C:\work\MARS\Mars.DB",
    "C:\work\MARS\MarsCInjector",
    "C:\work\MARS\VirtualAgentCaptureScreen",
    "C:\work\MARS\Mars.AutoTestingDriver",
    "C:\work\MARS\TestFrameMonitor",
    "C:\work\MARS\MarsExceptionManagement",
    "C:\work\MARS\MARS.AI.NLP.interface",
    "C:\work\MARS\MarsSpyTool",
    "C:\work\MARS\MarsMobileEngine",
    "C:\work\MARS\MarsLicense",
    "C:\work\MARS\AppiumCSharp",       
    "C:\work\MARS\MarsUnitTest",
    "C:\work\MARS\MarsDllInjectorHost",
    "C:\work\MARS\MarsLicense"
)

foreach ($dir in $RepoDirs) {
    if (-not (Test-Path "$dir\.git")) {
        Write-Host "跳过：$dir 不是一个git仓库" -ForegroundColor Yellow
        continue
    }
    Write-Host "`n处理仓库: $dir" -ForegroundColor Cyan
    Push-Location $dir

    # 获取当前分支名
    $currentBranch = git rev-parse --abbrev-ref HEAD
    Write-Host "当前分支: $currentBranch"
    Write-Host "将要合并本地分支: $SourceBranch"

    # 检查本地分支是否存在
    $branchExists = git branch --list $SourceBranch
    if (-not $branchExists) {
        Write-Host "本地分支 $SourceBranch 不存在，跳过。" -ForegroundColor Red
        Pop-Location
        continue
    }

    # 合并本地分支到当前分支
    git merge $SourceBranch

    if ($LASTEXITCODE -eq 0) {
        Write-Host "分支 $SourceBranch 已成功合并到 $currentBranch" -ForegroundColor Green
    } else {
        Write-Host "合并过程中出现冲突或错误，请手动处理。" -ForegroundColor Red
    }

    Pop-Location
}

Write-Host "`n所有仓库已执行 merge 操作。" -ForegroundColor Green
