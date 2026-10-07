param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repo "revit/DtoB.Revit2025/bin/$Configuration/net8.0-windows"
if (!(Test-Path -LiteralPath (Join-Path $source 'DtoB.Revit2025.dll'))) { throw 'Build the connector first.' }
if (Get-Process Revit -ErrorAction SilentlyContinue) { throw 'Close Revit before installing/updating the connector.' }
$destination = Join-Path $env:LOCALAPPDATA 'DtoB/phase00/connector'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$manifestDirectory = Join-Path $env:APPDATA 'Autodesk/Revit/Addins/2025'
New-Item -ItemType Directory -Path $manifestDirectory -Force | Out-Null
$manifestPath = Join-Path $manifestDirectory 'DtoB.Phase00.addin'
[xml]$manifest = Get-Content -LiteralPath (Join-Path $repo 'revit/DtoB.Revit2025/DtoB.addin.template') -Raw
if (Test-Path -LiteralPath $manifestPath) {
    [xml]$existing = Get-Content -LiteralPath $manifestPath -Raw
    if ($existing.RevitAddIns.AddIn.AddInId -ne $manifest.RevitAddIns.AddIn.AddInId) { throw 'Manifest path belongs to another add-in; not overwritten.' }
}
$files = Get-ChildItem -LiteralPath $source -File | Where-Object { $_.Name -like 'DtoB.*' -and ($_.Extension -eq '.dll' -or $_.Name.EndsWith('.deps.json')) }
$hashes = foreach ($file in $files) {
    Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
    $deployed = Join-Path $destination $file.Name
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    $deployedHash = (Get-FileHash -LiteralPath $deployed -Algorithm SHA256).Hash
    if ($sourceHash -ne $deployedHash) { throw "Deployment hash mismatch: $($file.Name)" }
    [pscustomobject]@{ file = $file.Name; sourceSha256 = $sourceHash; deployedSha256 = $deployedHash }
}
@{ utc = [DateTimeOffset]::UtcNow; files = @($hashes) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination 'deployment-manifest.json') -Encoding utf8
$manifest.RevitAddIns.AddIn.Assembly = Join-Path $destination 'DtoB.Revit2025.dll'
$manifest.Save($manifestPath)
Write-Output "Installed manifest: $manifestPath"
Write-Output "Connector: $($manifest.RevitAddIns.AddIn.Assembly)"
