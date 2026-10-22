<#
.SYNOPSIS
    Crea lo zip di rilascio: test, exe singolo, zip e impronta SHA-256 in dist\.

.DESCRIPTION
    La versione si legge da <Version> in ControlSaveManager.csproj.
    Funziona con Windows PowerShell 5.1 e con PowerShell 7.

.PARAMETER SkipTests
    Non esegue i test (solo per prove veloci).

.PARAMETER AllowDirty
    Crea lo zip anche con modifiche non committate.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\release.ps1
#>
param(
    [switch]$SkipTests,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'ControlSaveManager.csproj'
$tests = Join-Path $root 'tests\ControlSaveManager.Tests'
$publishDir = Join-Path $root 'bin\publish\win-x64'
$dist = Join-Path $root 'dist'

function Invoke-Checked([string]$what, [scriptblock]$command) {
    Write-Host "== $what" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "$what non riuscito (codice $LASTEXITCODE)." }
}

# Uno zip deve corrispondere a un commit preciso.
if (-not $AllowDirty) {
    $status = git -C $root status --porcelain
    if ($status) { throw "Ci sono modifiche non committate. Fai il commit o usa -AllowDirty." }
}

$version = (dotnet msbuild $project -getProperty:Version).Trim()
if (-not $version) { throw "Versione non trovata nel .csproj." }

$name = "ControlSaveManager-$version-win-x64"
Write-Host "Rilascio $name" -ForegroundColor Green

if (-not $SkipTests) {
    Invoke-Checked 'Test' { dotnet test $tests -c Release --nologo }
}

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
Invoke-Checked 'Pubblicazione' { dotnet publish $project -p:PublishProfile=win-x64 --nologo }

$exe = Join-Path $publishDir 'ControlSaveManager.exe'
if (-not (Test-Path $exe)) { throw "Exe non trovato: $exe" }

# Contenuto dello zip: l'exe e il README.
$staging = Join-Path $dist $name
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null
Copy-Item $exe $staging
Copy-Item (Join-Path $root 'README.md') $staging

$zip = Join-Path $dist "$name.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip
Remove-Item $staging -Recurse -Force

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$zip.sha256" -Value "$hash  $name.zip" -Encoding ascii

$sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host ""
Write-Host "Creato: $zip ($sizeMb MB)" -ForegroundColor Green
Write-Host "SHA-256: $hash"
