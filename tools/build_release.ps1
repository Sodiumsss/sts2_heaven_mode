$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dotnet = "dotnet"
$godot = "C:\Program Files\megadot\MegaDot_v4.5.1-stable_mono_win64_console.exe"
$buildRoot = Join-Path $root "build"
$releaseDir = Join-Path $root "build\HeavenMode"
$dllSource = Join-Path $root ".godot\mono\temp\bin\Debug\HeavenMode.dll"
$pckSource = Join-Path $root "build\HeavenMode.pck"
$manifestPath = Join-Path $root "mod_manifest.json"
$modAssetDir = Join-Path $root "HeavenMode"
$modImageTarget = Join-Path $modAssetDir "mod_image.png"

function Sync-ModImageFromRef {
  $refImage = Get-ChildItem -Path (Join-Path $root "ref") -Recurse -Filter "mod_image.png" -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($null -eq $refImage) { return }

  New-Item -ItemType Directory -Force -Path $modAssetDir | Out-Null
  Copy-Item $refImage.FullName -Destination $modImageTarget -Force

  $importPath = "$modImageTarget.import"
  if (Test-Path $importPath) { Remove-Item $importPath -Force }
  Get-ChildItem -Path (Join-Path $root ".godot\imported") -Filter "mod_image.png-*" -File -ErrorAction SilentlyContinue | Remove-Item -Force
}

Write-Host "==> dotnet build"
& $dotnet build (Join-Path $root "HeavenMode.csproj") -c Debug

Write-Host "==> sync mod image"
Sync-ModImageFromRef

Write-Host "==> import project assets"
& $godot --headless --path $root --import --quit

Write-Host "==> build pck"
& $godot --headless --path $root --script "res://tools/build_pck.gd"

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Get-ChildItem -Path $releaseDir -Force | Remove-Item -Recurse -Force
Get-ChildItem -Path $buildRoot -Filter "HeavenMode-*.zip" -File -ErrorAction SilentlyContinue | Remove-Item -Force

Copy-Item $dllSource -Destination (Join-Path $releaseDir "HeavenMode.dll") -Force
Copy-Item $pckSource -Destination (Join-Path $releaseDir "HeavenMode.pck") -Force

$manifestJson = [System.IO.File]::ReadAllText($manifestPath, [System.Text.Encoding]::UTF8)
$manifest = $manifestJson | ConvertFrom-Json
$version = [string]$manifest.version
$modFolderName = if ([string]::IsNullOrWhiteSpace([string]$manifest.pck_name)) { [string]$manifest.name } else { [string]$manifest.pck_name }
if ([string]::IsNullOrWhiteSpace($version)) { throw "mod_manifest.json missing version field" }
if ([string]::IsNullOrWhiteSpace($modFolderName)) { throw "mod_manifest.json missing name/pck_name field" }

Copy-Item $manifestPath -Destination (Join-Path $releaseDir "mod_manifest.json") -Force

$newManifestPath = Join-Path $releaseDir "$modFolderName.json"
$newManifest = [ordered]@{
  id               = $modFolderName
  name             = [string]$manifest.name
  author           = [string]$manifest.author
  description      = [string]$manifest.description
  version          = [string]$manifest.version
  has_pck          = $true
  has_dll          = $true
  dependencies     = @()
  affects_gameplay = $true
}
$newManifestJson = $newManifest | ConvertTo-Json -Depth 3
[System.IO.File]::WriteAllText($newManifestPath, $newManifestJson, [System.Text.UTF8Encoding]::new($false))

$zipName = "HeavenMode-$version.zip"
$zipPath = Join-Path $buildRoot $zipName
$zipStageRoot = Join-Path $buildRoot "_zip_stage"
$zipModFolder = Join-Path $zipStageRoot $modFolderName

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (Test-Path $zipStageRoot) { Remove-Item $zipStageRoot -Recurse -Force }

New-Item -ItemType Directory -Force -Path $zipModFolder | Out-Null
Copy-Item (Join-Path $releaseDir "*") -Destination $zipModFolder -Recurse -Force

Compress-Archive -Path $zipModFolder -DestinationPath $zipPath -CompressionLevel Optimal
Remove-Item $zipStageRoot -Recurse -Force

Write-Host "==> Release built: $zipPath"

PAUSE
