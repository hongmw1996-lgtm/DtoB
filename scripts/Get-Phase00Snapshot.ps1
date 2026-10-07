param([string]$OutputPath)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$files=@()
foreach ($folder in @('apps','packages','services','revit','tests','datasets/controlled/phase00','scripts')) {
    $files+=Get-ChildItem -LiteralPath (Join-Path $repo $folder) -File -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in '.cs','.csproj','.xaml','.json','.ps1','.template' }
}
$files+=@('DtoB.sln','Directory.Build.props','global.json') | ForEach-Object { Get-Item -LiteralPath (Join-Path $repo $_) }
$entries=@($files | Sort-Object FullName -Unique | ForEach-Object { [pscustomobject]@{ path=[IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/'); sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
$canonical=($entries | ForEach-Object { $_.path+' '+$_.sha256 }) -join "`n"
$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($canonical)))
Push-Location $repo
try { $head=(& git rev-parse HEAD); $status=@(& git status --porcelain) } finally { Pop-Location }
$result=@{ schemaVersion=1; utc=[DateTimeOffset]::UtcNow; baseCommit=$head; workingTreeModified=($status.Count -gt 0); sourceTreeSha256=$hash; files=$entries }
if($OutputPath){$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $OutputPath -Encoding utf8}
$result
