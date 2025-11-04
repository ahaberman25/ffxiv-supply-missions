param(
  [string]$Configuration = "Release",
  [string]$Version = ""
)

$ErrorActionPreference = "Stop"

# Paths
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $repoRoot

# Resolve version
if ([string]::IsNullOrWhiteSpace($Version)) {
  # Use existing helper to read repo version
  $Version = & "$PSScriptRoot\version.ps1"
}
if ([string]::IsNullOrWhiteSpace($Version)) { $Version = "0.0.1" }

Write-Host "Version detected: $Version"

# Build with an in-memory version override (doesn't touch files in repo)
# dotnet honors /p:Version to set AssemblyInformationalVersion & package version
Write-Host "Building SupplyMissionHelper ($Configuration) with /p:Version=$Version ..."
dotnet build .\SupplyMissionHelper.csproj -c $Configuration /p:Version=$Version

# Output / staging
$outDir = Join-Path $repoRoot "bin\$Configuration"
$distRoot = Join-Path $repoRoot "dist"
$dist     = Join-Path $distRoot "SupplyMissionHelper"
$zipOut   = Join-Path $distRoot "SupplyMissionHelper-$Version.zip"

# Clean dist
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
if (-not (Test-Path $distRoot)) { New-Item $distRoot -ItemType Directory | Out-Null }
New-Item $dist -ItemType Directory | Out-Null

# Copy built DLL/PDB
Copy-Item "$outDir\SupplyMissionHelper.dll" $dist
if (Test-Path "$outDir\SupplyMissionHelper.pdb") {
  Copy-Item "$outDir\SupplyMissionHelper.pdb" $dist
}

# Write a manifest to staging with the CI version baked in (but do not edit repo)
$manifestPath = ".\manifest.json"
if (Test-Path $manifestPath) {
  $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
  $manifest.EffectiveVersion = $Version
  # ensure minimal formatting to avoid BOM/encoding woes
  $manifest | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $dist "manifest.json") -Encoding UTF8
} else {
  throw "manifest.json not found at repo root."
}

# Zip staging folder
if (Test-Path $zipOut) { Remove-Item $zipOut -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($dist, $zipOut)

Write-Host "Packed → $zipOut"
