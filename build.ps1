<#
.SYNOPSIS
    Builds the Kassel Performance S54 VANOS Tester using MSBuild (no 'dotnet' CLI required).

.DESCRIPTION
    This is a .NET Framework 4.8 WinForms app, so it builds with the classic MSBuild that ships
    with Visual Studio / Build Tools. This script locates MSBuild automatically via vswhere and
    falls back to known install paths, so you do NOT need a working 'dotnet' command.

.PARAMETER Configuration
    Build configuration: Release (default) or Debug.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build.ps1
    powershell -ExecutionPolicy Bypass -File build.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'S54VanosTester.csproj'

function Find-MSBuild {
    # 1. Preferred: ask vswhere (ships with VS 2017+ at a fixed location).
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -prerelease `
            -requires Microsoft.Component.MSBuild `
            -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($path -and (Test-Path $path)) { return $path }
    }

    # 2. Fallback: probe well-known install locations for VS 2019/2022 (and Build Tools).
    $bases = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019"
    )
    foreach ($base in $bases) {
        foreach ($edition in 'Enterprise', 'Professional', 'Community', 'BuildTools') {
            $candidate = Join-Path $base "$edition\MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $candidate) { return $candidate }
        }
    }

    # 3. Last resort: MSBuild on PATH.
    $cmd = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    return $null
}

$msbuild = Find-MSBuild
if (-not $msbuild) {
    Write-Error @"
Could not find MSBuild. Install Visual Studio 2019/2022 or the free
'Build Tools for Visual Studio' with the '.NET desktop build tools' workload:
  https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
"@
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

& $msbuild $project /t:Restore,Build /p:Configuration=$Configuration /p:Platform=x86 /v:minimal /nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed (exit code $LASTEXITCODE)."
    exit $LASTEXITCODE
}

$output = Join-Path $root "bin\$Configuration\net48"
Write-Host ""
Write-Host "Build succeeded." -ForegroundColor Green
Write-Host "Output: $output\S54VanosTester.exe"
