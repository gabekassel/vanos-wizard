<#
.SYNOPSIS
    Removes build artifacts (bin, obj) and packaging output (dist) from the project.

.DESCRIPTION
    Does NOT touch source, the runtime\EDIABAS bundle, or appsettings.json. Safe to run anytime.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File clean.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$targets = @('bin', 'obj', 'dist')
foreach ($t in $targets) {
    $path = Join-Path $root $t
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
        Write-Host "Removed $t\" -ForegroundColor Green
    } else {
        Write-Host "Skipped $t\ (not present)" -ForegroundColor DarkGray
    }
}

Write-Host "Clean complete." -ForegroundColor Cyan
