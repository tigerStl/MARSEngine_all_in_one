# ========================================
# MARS Spy Tool - 嵌入式 License 构建脚本
# ========================================
# 
# 用途：为特定客户生成包含嵌入 License 的定制版本
# 使用：.\build_with_embedded_license.ps1 -CustomerName "客户名" -LicenseType "Enterprise"

param(
    [Parameter(Mandatory=$true)]
    [string]$CustomerName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Trial", "Standard", "Professional", "Enterprise", "Perpetual")]
    [string]$LicenseType = "Professional",
    
    [Parameter(Mandatory=$false)]
    [int]$Years = 1,
    
    [Parameter(Mandatory=$false)]
    [int]$MaxActivations = 1,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

# 标题
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "    MARS Spy Tool - 嵌入式 License 构建工具" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 显示参数
Write-Host "构建参数：" -ForegroundColor Yellow
Write-Host "  客户名称: $CustomerName" -ForegroundColor White
Write-Host "  License 类型: $LicenseType" -ForegroundColor White
Write-Host "  有效期: $Years 年" -ForegroundColor White
Write-Host "  最大激活次数: $MaxActivations" -ForegroundColor White
Write-Host "  编译配置: $Configuration" -ForegroundColor White
Write-Host ""

# 确认
$confirmation = Read-Host "是否继续? (Y/N)"
if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
    Write-Host "已取消构建" -ForegroundColor Yellow
    exit 0
}

# 步骤 1: 检查必要的工具和文件
Write-Host ""
Write-Host "步骤 1: 检查环境..." -ForegroundColor Yellow

$projectFile = "MarsSpyTool.csproj"
$resourcesDir = "Resources"
$outputDir = "bin\$Configuration"
$releasesDir = "Releases"

if (-not (Test-Path $projectFile)) {
    Write-Host "错误: 未找到项目文件 $projectFile" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $resourcesDir)) {
    Write-Host "创建 Resources 目录..." -ForegroundColor White
    New-Item -ItemType Directory -Path $resourcesDir | Out-Null
}

if (-not (Test-Path $releasesDir)) {
    Write-Host "创建 Releases 目录..." -ForegroundColor White
    New-Item -ItemType Directory -Path $releasesDir | Out-Null
}

Write-Host "✓ 环境检查完成" -ForegroundColor Green

# 步骤 2: 生成 License（这里需要实际的 License 生成器）
Write-Host ""
Write-Host "步骤 2: 生成 License Key..." -ForegroundColor Yellow

# 注意：这里假设您已经有一个 License 生成器工具
# 实际使用时，您需要：
# 1. 创建一个独立的 License 生成器控制台程序
# 2. 或者在这里调用 C# 代码

# 模拟生成（实际应该调用真实的生成器）
Write-Host "提示: 请手动运行 License 生成器，生成 $CustomerName 的 $LicenseType License" -ForegroundColor Cyan
Write-Host ""
Write-Host "生成器命令示例:" -ForegroundColor White
Write-Host "  LicenseGenerator.exe --customer `"$CustomerName`" --type $LicenseType --years $Years --output `".\Resources\embedded_license.lic`"" -ForegroundColor Gray
Write-Host ""

$licenseFile = Join-Path $resourcesDir "embedded_license.lic"

# 检查是否已经存在 License 文件
if (-not (Test-Path $licenseFile)) {
    Write-Host "警告: 未找到 License 文件 $licenseFile" -ForegroundColor Yellow
    Write-Host "请先生成 License 文件，或使用现有的 mars.lic 文件" -ForegroundColor Yellow
    
    # 如果存在 mars.lic，提示复制
    if (Test-Path "mars.lic") {
        $copy = Read-Host "发现 mars.lic 文件，是否复制为嵌入式 License? (Y/N)"
        if ($copy -eq 'Y' -or $copy -eq 'y') {
            Copy-Item "mars.lic" $licenseFile
            Write-Host "✓ License 文件已复制" -ForegroundColor Green
        } else {
            Write-Host "已取消构建" -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host "构建中止：需要 License 文件" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✓ 找到 License 文件: $licenseFile" -ForegroundColor Green
}

# 步骤 3: 更新项目文件，确保 License 文件作为嵌入资源
Write-Host ""
Write-Host "步骤 3: 配置项目..." -ForegroundColor Yellow

$projectContent = Get-Content $projectFile -Raw

# 检查是否已经包含嵌入资源配置
if ($projectContent -notmatch 'embedded_license\.lic') {
    Write-Host "添加嵌入资源配置到项目文件..." -ForegroundColor White
    
    # 在项目文件中添加嵌入资源（需要手动或使用 XML 解析）
    Write-Host "提示: 请确保在 .csproj 中添加以下配置:" -ForegroundColor Cyan
    Write-Host @"
  <ItemGroup>
    <EmbeddedResource Include="Resources\embedded_license.lic" />
  </ItemGroup>
"@ -ForegroundColor Gray
    Write-Host ""
    
    # 可选：自动添加（需要谨慎）
    # $insertPosition = $projectContent.LastIndexOf("</Project>")
    # $embeddedResourceXml = "  <ItemGroup>`n    <EmbeddedResource Include=`"Resources\embedded_license.lic`" />`n  </ItemGroup>`n"
    # $projectContent = $projectContent.Insert($insertPosition, $embeddedResourceXml)
    # Set-Content $projectFile $projectContent
}

Write-Host "✓ 项目配置完成" -ForegroundColor Green

# 步骤 4: 编译项目
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "步骤 4: 编译项目..." -ForegroundColor Yellow
    
    # 查找 MSBuild
    $msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    
    if (-not $msbuildPath) {
        # 尝试使用 dotnet
        Write-Host "使用 dotnet build..." -ForegroundColor White
        & dotnet build $projectFile /p:Configuration=$Configuration /v:minimal
    } else {
        Write-Host "使用 MSBuild: $msbuildPath" -ForegroundColor White
        & $msbuildPath $projectFile /p:Configuration=$Configuration /v:minimal
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ 编译成功" -ForegroundColor Green
    } else {
        Write-Host "✗ 编译失败" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host ""
    Write-Host "步骤 4: 跳过编译 (使用 -SkipBuild)" -ForegroundColor Yellow
}

# 步骤 5: 验证嵌入的 License
Write-Host ""
Write-Host "步骤 5: 验证嵌入的 License..." -ForegroundColor Yellow

$exePath = Join-Path $outputDir "MarsSpyTool.exe"
if (Test-Path $exePath) {
    Write-Host "✓ 找到可执行文件: $exePath" -ForegroundColor Green
    
    # 使用 .NET 反射检查嵌入资源（可选）
    # 这里简化处理
    Write-Host "提示: 请手动测试程序以验证 License 是否正确嵌入" -ForegroundColor Cyan
} else {
    Write-Host "警告: 未找到编译输出: $exePath" -ForegroundColor Yellow
}

# 步骤 6: 创建发布包
Write-Host ""
Write-Host "步骤 6: 创建发布包..." -ForegroundColor Yellow

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$sanitizedCustomerName = $CustomerName -replace '[\\/:*?"<>|]', '_'
$packageName = "MarsSpyTool_${sanitizedCustomerName}_${LicenseType}_${timestamp}.zip"
$packagePath = Join-Path $releasesDir $packageName

try {
    # 压缩输出目录
    Compress-Archive -Path "$outputDir\*" -DestinationPath $packagePath -Force
    Write-Host "✓ 发布包已创建: $packagePath" -ForegroundColor Green
    
    # 显示文件大小
    $fileSize = (Get-Item $packagePath).Length / 1MB
    Write-Host "  文件大小: $($fileSize.ToString('0.00')) MB" -ForegroundColor White
} catch {
    Write-Host "创建发布包时出错: $_" -ForegroundColor Red
}

# 步骤 7: 生成交付文档
Write-Host ""
Write-Host "步骤 7: 生成交付文档..." -ForegroundColor Yellow

$docPath = Join-Path $releasesDir "MarsSpyTool_${sanitizedCustomerName}_交付说明.txt"
$docContent = @"
═══════════════════════════════════════════════════════════
    MARS Spy Tool - 交付说明
═══════════════════════════════════════════════════════════

客户名称: $CustomerName
License 类型: $LicenseType
有效期: $Years 年
最大激活次数: $MaxActivations
构建时间: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
构建配置: $Configuration
发布包: $packageName

───────────────────────────────────────────────────────────
License 信息
───────────────────────────────────────────────────────────

此版本已预授权，无需手动激活。

License 特性：
$(if ($LicenseType -eq "Trial") { "- 试用期 30 天`n- 基础功能" })
$(if ($LicenseType -eq "Standard") { "- 有效期 $Years 年`n- 标准功能" })
$(if ($LicenseType -eq "Professional") { "- 有效期 $Years 年`n- 专业功能（包括录制回放）" })
$(if ($LicenseType -eq "Enterprise") { "- 有效期 $Years 年`n- 企业功能（全部功能）" })
$(if ($LicenseType -eq "Perpetual") { "- 永久有效`n- 全部功能" })

───────────────────────────────────────────────────────────
安装说明
───────────────────────────────────────────────────────────

1. 解压发布包到目标目录
2. 运行 MarsSpyTool.exe
3. 程序会自动使用嵌入的 License，无需手动激活

───────────────────────────────────────────────────────────
注意事项
───────────────────────────────────────────────────────────

1. 此版本为定制版本，仅授权给 $CustomerName 使用
2. 请勿分发给其他组织或个人
3. License 已绑定到程序集中，无法转移
4. 如需续费或升级，请联系销售

───────────────────────────────────────────────────────────
技术支持
───────────────────────────────────────────────────────────

Email: support@mars.com
电话: 400-xxx-xxxx
网站: https://www.mars.com/support

═══════════════════════════════════════════════════════════
"@

Set-Content -Path $docPath -Value $docContent -Encoding UTF8
Write-Host "✓ 交付文档已创建: $docPath" -ForegroundColor Green

# 完成
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "    构建完成!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "交付物：" -ForegroundColor Yellow
Write-Host "  1. 发布包: $packagePath" -ForegroundColor White
Write-Host "  2. 交付文档: $docPath" -ForegroundColor White
Write-Host ""
Write-Host "下一步：" -ForegroundColor Yellow
Write-Host "  1. 测试程序确保 License 正常工作" -ForegroundColor White
Write-Host "  2. 将发布包和文档发送给客户" -ForegroundColor White
Write-Host "  3. 记录交付信息到 License 管理系统" -ForegroundColor White
Write-Host ""
Write-Host "提示: 建议在虚拟机或测试环境中验证程序" -ForegroundColor Cyan
Write-Host ""

