$path = Join-Path $PSScriptRoot '..\Mars.AutoTestingDriver-WebClient.csproj'
$path = [System.IO.Path]::GetFullPath($path)
$content = Get-Content -Raw -Path $path

if ($content -notmatch 'Target Name="SelectEnvironmentAppConfig"') {
$target = @'
  <Target Name="SelectEnvironmentAppConfig" AfterTargets="FindAppConfigFile">
    <ItemGroup>
      <AppConfigWithTargetPath Remove="@(AppConfigWithTargetPath)" />
      <AppConfigWithTargetPath Include="$(SelectedAppConfig)">
        <TargetPath>$(TargetFileName).config</TargetPath>
      </AppConfigWithTargetPath>
    </ItemGroup>
    <PropertyGroup>
      <AppConfig>$(SelectedAppConfig)</AppConfig>
    </PropertyGroup>
  </Target>
'@

$marker = @'
  <Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">
'@

$content = $content.Replace($marker, "$target`r`n$marker")
[System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
}
