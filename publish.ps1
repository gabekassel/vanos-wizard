<#
.SYNOPSIS
    Builds and packages the Kassel Performance S54 VANOS Tester into a standalone, zippable folder.

.DESCRIPTION
    Runs a Release build via build.ps1 (MSBuild, no 'dotnet' required), checks that the portable
    EDIABAS runtime was bundled into the output, and produces a versioned .zip you can copy to any
    Windows 10/11 machine and run by double-clicking the .exe -- no EDIABAS install needed.

.PARAMETER OutDir
    Folder to write the .zip into. Default: dist\

.PARAMETER SkipBuild
    Package the existing build output without rebuilding.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File publish.ps1
#>
[CmdletBinding()]
param(
    [string]$OutDir,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$config = 'Release'
$buildOutput = Join-Path $root "bin\$config\net48"
if (-not $OutDir) { $OutDir = Join-Path $root 'dist' }

# 1. Build (unless skipping).
if (-not $SkipBuild) {
    & (Join-Path $root 'build.ps1') -Configuration $config
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$exe = Join-Path $buildOutput 'S54VanosTester.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Build output not found at $exe. Run a build first (omit -SkipBuild)."
    exit 1
}

# 2. Verify the portable EDIABAS runtime made it into the output.
$bundledApi = Join-Path $buildOutput 'EDIABAS\BIN\api32.dll'
if (Test-Path $bundledApi) {
    Write-Host "Portable EDIABAS runtime bundled: OK (EDIABAS\BIN\api32.dll present)." -ForegroundColor Green
    $portable = $true
} else {
    Write-Warning @"
The portable EDIABAS runtime is NOT bundled (EDIABAS\BIN\api32.dll missing in the output).
The package will require an EDIABAS installation on the target machine.
To make it standalone, copy your licensed EDIABAS files into:
    runtime\EDIABAS\BIN\   (must contain api32.dll)
    runtime\EDIABAS\ECU\   (MSS54.PRG + group files)
...then re-run this script. See README.md.
"@
    $portable = $false
}

# 3. Determine version from the built assembly for the zip name.
$version = (Get-Item $exe).VersionInfo.FileVersion
if (-not $version) { $version = '1.0.0.0' }

$tag = if ($portable) { 'standalone' } else { 'needs-ediabas' }
$zipName = "KasselPerformance-S54VanosTester-v$version-$tag.zip"

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
$zipPath = Join-Path $OutDir $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# 4. Zip the entire build output folder.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $buildOutput, $zipPath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)   # do not nest under an extra top-level folder

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Package created." -ForegroundColor Green
Write-Host "  $zipPath  ($sizeMB MB)"
Write-Host ""
if ($portable) {
    Write-Host "This is a STANDALONE package: unzip and double-click S54VanosTester.exe." -ForegroundColor Green
} else {
    Write-Host "This package REQUIRES EDIABAS installed on the target machine." -ForegroundColor Yellow
}
