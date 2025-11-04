# Try Git tag first
$tag = (git describe --tags --abbrev=0 2>$null)
if ($tag) {
    $cleanTag = $tag -replace '^v', ''   # strip leading v
    return $cleanTag
}

# fallback: manifest.json > csproj
$manifest = Get-Content "..\manifest.json" -Raw | ConvertFrom-Json
if ($manifest.EffectiveVersion) { return $manifest.EffectiveVersion }

[xml]$csproj = Get-Content "..\SupplyMissionHelper.csproj"
return $csproj.Project.PropertyGroup.Version ?? "0.0.1"
