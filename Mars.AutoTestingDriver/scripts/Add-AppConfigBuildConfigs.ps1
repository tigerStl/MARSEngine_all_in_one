param(
    [string]$ProjectFile = (Join-Path $PSScriptRoot '..\Mars.AutoTestingDriver-WebClient.csproj')
)

$ErrorActionPreference = 'Stop'

if (-not [System.IO.Path]::IsPathRooted($ProjectFile)) {
    $ProjectFile = Join-Path $PSScriptRoot $ProjectFile
}

$ProjectFile = [System.IO.Path]::GetFullPath($ProjectFile)

if (-not (Test-Path -LiteralPath $ProjectFile)) {
    throw "Project file not found: $ProjectFile"
}

function Add-BlockAfter {
    param(
        [string]$Content,
        [string]$Anchor,
        [string]$Block,
        [string]$PresenceMarker,
        [string]$Description
    )

    if ($Content.Contains($PresenceMarker)) {
        Write-Host "Skip: $Description already exists." -ForegroundColor Yellow
        return $Content
    }

    if (-not $Content.Contains($Anchor)) {
        throw "Anchor not found for $Description"
    }

    Write-Host "Add: $Description" -ForegroundColor Green
    return $Content.Replace($Anchor, $Anchor + [Environment]::NewLine + $Block)
}

function Replace-Block {
    param(
        [string]$Content,
        [string]$OldBlock,
        [string]$NewBlock,
        [string]$PresenceMarker,
        [string]$Description
    )

    if ($Content.Contains($PresenceMarker)) {
        Write-Host "Skip: $Description already exists." -ForegroundColor Yellow
        return $Content
    }

    if (-not $Content.Contains($OldBlock)) {
        throw "Original block not found for $Description"
    }

    Write-Host "Update: $Description" -ForegroundColor Green
    return $Content.Replace($OldBlock, $NewBlock)
}

$content = [System.IO.File]::ReadAllText($ProjectFile)

$debugWebBlock = @'
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'DEBUG FOR WEBVERSION|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome_1</DefineConstants>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome;_demo_for_14;mars_Agent_no</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
'@

$debugEnvBlocks = @'
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'DebugHundsunDemo|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome;_demo_for_14;mars_Agent_no</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug_CICDTEST|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome;_demo_for_14;mars_Agent_no</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug_FHLB|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome;_demo_for_14;mars_Agent_no</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug_MARS26|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;DEBUG;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_EnableChrome;_demo_for_14;mars_Agent_no</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
    <Prefer32Bit>false</Prefer32Bit>
  </PropertyGroup>
'@

$content = Add-BlockAfter -Content $content -Anchor $debugWebBlock -Block $debugEnvBlocks -PresenceMarker "DebugHundsunDemo|AnyCPU" -Description 'debug app config build configurations'

$hundsunReleaseBlock = @'
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'ReleaseHundsunDemo|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_demoLicense_1;_EnableChrome;_demo_for_14</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
'@

$hundsunReleaseWithX64 = @'
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'ReleaseHundsunDemo|AnyCPU'">
    <DebugSymbols>true</DebugSymbols>
    <OutputPath>..\..\automationTest\Automation Workbooks\dlls\</OutputPath>
    <DefineConstants>TRACE;_MarsCDriver;_Datafrom_Database;_remoteDebug_1;_EngineDriver;_forWebClient;_noLocalApplications;_demoLicense_1;_EnableChrome;_demo_for_14</DefineConstants>
    <DebugType>full</DebugType>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>9.0</LangVersion>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>MinimumRecommendedRules.ruleset</CodeAnalysisRuleSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'ReleaseHundsunDemo|x64'">
    <OutputPath>bin\x64\ReleaseHundsunDemo\</OutputPath>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
'@

$content = Replace-Block -Content $content -OldBlock $hundsunReleaseBlock -NewBlock $hundsunReleaseWithX64 -PresenceMarker "ReleaseHundsunDemo|x64" -Description 'HundsunDemo x64 release configuration'

$mappingBlock = @'
    <PropertyGroup>
      <EnvConfig Condition="$(ConfigUpper.Contains('CICDTEST'))">App.cicdtest.config</EnvConfig>
      <EnvConfig Condition="$(ConfigUpper.Contains('MARS26'))">App.mars26.config</EnvConfig>
      <EnvConfig Condition="$(ConfigUpper.Contains('FHLB'))">App.fhlb.config</EnvConfig>
    </PropertyGroup>
'@

$mappingWithHundsun = @'
    <PropertyGroup>
      <EnvConfig Condition="$(ConfigUpper.Contains('CICDTEST'))">App.cicdtest.config</EnvConfig>
      <EnvConfig Condition="$(ConfigUpper.Contains('MARS26'))">App.mars26.config</EnvConfig>
      <EnvConfig Condition="$(ConfigUpper.Contains('FHLB'))">App.fhlb.config</EnvConfig>
      <EnvConfig Condition="$(ConfigUpper.Contains('HUNDSUNDEMO'))">App.HundsunDemo.config</EnvConfig>
    </PropertyGroup>
'@

$content = Replace-Block -Content $content -OldBlock $mappingBlock -NewBlock $mappingWithHundsun -PresenceMarker "$(ConfigUpper.Contains('HUNDSUNDEMO'))" -Description 'HundsunDemo app config mapping'

$backupFile = '{0}.{1}.bak' -f $ProjectFile, (Get-Date -Format 'yyyyMMddHHmmss')
Copy-Item -LiteralPath $ProjectFile -Destination $backupFile -Force
Write-Host "Backup created: $backupFile" -ForegroundColor Cyan

$utf8Bom = New-Object System.Text.UTF8Encoding($true)
[System.IO.File]::WriteAllText($ProjectFile, $content, $utf8Bom)

Write-Host "Updated: $ProjectFile" -ForegroundColor Cyan
Write-Host 'Done. Please reopen the project in Visual Studio after running this script.' -ForegroundColor Cyan
