<#
Builds SAPAutoLogon.plgx (KeePass plugin package) into the repository root.

KeePass's PLGX compiler needs a classic (non-SDK) .csproj with an explicit file list,
so this script copies the plugin sources into a temporary folder, writes such a
.csproj there, and runs KeePass.exe --plgx-create on that folder. Nothing in the
repository or in the KeePass install folder is modified.

Usage:  powershell -File packaging\build-plgx.ps1 [-KeePassExe "C:\...\KeePass.exe"]
#>
param(
    [string]$KeePassExe = 'C:\Program Files\KeePass Password Safe 2\KeePass.exe',
    [string]$WorkDir = (Join-Path $env:TEMP 'sapautologon-plgx')
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$src = Join-Path $repo 'SAPAutoLogon'

# $WorkDir is deleted recursively, so it must be a folder below the temp folder
$tempRoot = [IO.Path]::GetFullPath($env:TEMP).TrimEnd('\') + '\'
$WorkDir = [IO.Path]::GetFullPath($WorkDir).TrimEnd('\')
if (-not ($WorkDir + '\').StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or ($WorkDir + '\') -eq $tempRoot) {
    throw "WorkDir must be a folder below $env:TEMP, got: $WorkDir"
}
$plgx = "$WorkDir.plgx"

foreach ($stale in @($WorkDir, $plgx)) {
    if (Test-Path $stale) { Remove-Item -Recurse -Force $stale }
}

try {
New-Item -ItemType Directory -Path $WorkDir | Out-Null

# Copy the sources, leaving out build output and the SDK-style project file.
# The bin/obj test uses the path relative to the plugin folder, so a repository
# that itself lives below a folder named bin or obj still works.
Get-ChildItem -Path $src -Recurse -File |
    ForEach-Object {
        $rel = $_.FullName.Substring($src.Length + 1)
        $dirs = @($rel.Split([char]92) | Select-Object -SkipLast 1)
        if (($dirs -contains 'bin') -or ($dirs -contains 'obj') -or $rel -eq 'SAPAutoLogon.csproj') { return }
        $dest = Join-Path $WorkDir $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
        Copy-Item $_.FullName $dest
    }

$compile = Get-ChildItem -Path $WorkDir -Recurse -Filter *.cs | ForEach-Object {
    '    <Compile Include="' + $_.FullName.Substring($WorkDir.Length + 1) + '" />'
}

# .NET Framework assemblies the sources use (KeePass itself is added by the PLGX compiler).
# Keep this the single list: System.Net is in System, System.Linq in System.Core,
# XDocument (LandscapeReader, LandscapeLoader) in System.Xml.Linq.
$frameworkReferences = @(
    'System',
    'System.Core',
    'System.Drawing',
    'System.Windows.Forms',
    'System.Xml',
    'System.Xml.Linq'
)
$references = $frameworkReferences | ForEach-Object { '    <Reference Include="' + $_ + '" />' }

$csproj = @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration Condition=" '`$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '`$(Platform)' == '' ">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <RootNamespace>SAPAutoLogon</RootNamespace>
    <AssemblyName>SAPAutoLogon</AssemblyName>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
$($references -join "`r`n")
  </ItemGroup>
  <ItemGroup>
$($compile -join "`r`n")
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="OptionDialog.resx"><DependentUpon>OptionDialog.cs</DependentUpon></EmbeddedResource>
    <EmbeddedResource Include="Properties\Resources.resx" />
  </ItemGroup>
  <Import Project="`$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
"@
Set-Content -Path (Join-Path $WorkDir 'SAPAutoLogon.csproj') -Value $csproj -Encoding UTF8

# KeePass.exe is a GUI-subsystem program: wait for it explicitly and check the exit code
$kp = Start-Process -FilePath $KeePassExe -Wait -PassThru -ArgumentList @(
    '--plgx-create', ('"' + $WorkDir + '"'),
    '--plgx-prereq-kp:2.53', '--plgx-prereq-net:4.8', '--plgx-prereq-os:Windows')
if ($kp.ExitCode -ne 0) { throw "KeePass --plgx-create failed with exit code $($kp.ExitCode)" }
if (-not (Test-Path $plgx)) { throw "PLGX was not created: $plgx" }

Copy-Item $plgx (Join-Path $repo 'SAPAutoLogon.plgx') -Force
Write-Host "Created $(Join-Path $repo 'SAPAutoLogon.plgx')"
}
finally {
    foreach ($tmp in @($WorkDir, $plgx)) {
        if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue }
    }
}