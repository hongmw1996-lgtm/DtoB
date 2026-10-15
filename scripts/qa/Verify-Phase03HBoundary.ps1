param([string]$EvidenceDirectory)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(!$EvidenceDirectory){$EvidenceDirectory=Join-Path $repo 'artifacts/phase03/03h-viewer-20261015'}
$allowed=@('apps/DtoB.Desktop/MainWindow.xaml','apps/DtoB.Desktop/MainWindow.xaml.cs','apps/DtoB.Desktop/DtoB.Desktop.csproj','tests/DtoB.Tests/DtoB.Tests.csproj','tests/DtoB.Tests/FoundationDocumentTests.cs','docs/status/PHASE_03_CHECKLIST.md')
$before=Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'before.json') -Raw|ConvertFrom-Json
$results=foreach($entry in $before){
    $relative=$entry.Path.Replace('\','/');$path=Join-Path $repo $entry.Path
    $after=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if($entry.Hash -ne $after -and $relative -notin $allowed){throw "Unexpected prior-file change: $relative"}
    [ordered]@{Path=$relative;Before=$entry.Hash;After=$after;Unchanged=($entry.Hash -eq $after)}
}
$results|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'protected-hashes.json') -Encoding utf8
$prefix=[IO.File]::ReadAllText((Join-Path $EvidenceDirectory 'checklist-before.md'))
if(![IO.File]::ReadAllText((Join-Path $repo 'docs/status/PHASE_03_CHECKLIST.md')).StartsWith($prefix,[StringComparison]::Ordinal)){throw 'Checklist history changed.'}
$controlled=(Get-FileHash -LiteralPath (Join-Path $repo 'datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf')).Hash
if($controlled -ne '2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC'){throw 'Controlled source hash changed.'}
$snapshot=(Get-FileHash -LiteralPath (Join-Path $EvidenceDirectory 'final/controlled-snapshot.json')).Hash
if($snapshot -ne '4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C'){throw 'Snapshot regression hash changed.'}
$scene=@(Get-ChildItem (Join-Path $EvidenceDirectory 'final') -Filter 'scene-*.sha256'|ForEach-Object{(Get-Content -LiteralPath $_.FullName -Raw).Trim()})
if($scene.Count -ne 3 -or @($scene|Where-Object{$_ -ne '9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7'}).Count){throw 'Scene determinism evidence failed.'}
$dependency=foreach($name in 'DtoB.Cad.Raw','DtoB.Cad.Dxf','DtoB.Desktop'){
    $folder=if($name -eq 'DtoB.Desktop'){'apps'}else{'packages'}
    [xml]$project=Get-Content -LiteralPath (Join-Path $repo "$folder/$name/$name.csproj") -Raw
    $refs=@($project.SelectNodes('//ProjectReference')|ForEach-Object{[IO.Path]::GetFileNameWithoutExtension($_.Include)})
    if($project.SelectNodes('//PackageReference').Count){throw "Unexpected package: $name"}
    if($name -eq 'DtoB.Cad.Raw' -and $refs.Count){throw 'Reverse Raw dependency.'}
    if($name -eq 'DtoB.Cad.Dxf' -and ($refs.Count -ne 1 -or $refs[0] -ne 'DtoB.Cad.Raw')){throw 'Dxf dependency changed.'}
    [ordered]@{Project=$name;References=$refs;Packages=0}
}
$dependency|ConvertTo-Json -Depth 3|Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'dependency-audit.json') -Encoding utf8
$production=Get-ChildItem (Join-Path $repo 'packages/DtoB.Cad.Raw'),(Join-Path $repo 'packages/DtoB.Cad.Dxf') -File -Filter '*.cs'
$hits=@($production|Select-String -Pattern 'Math\.(Sin|Cos|Tan|Atan2)\s*\(|Matrix3D|ConvertToInternalUnits\s*\(|using\s+(Autodesk|System\.Windows|DtoB\.Project|DtoB\.Geometry|ACadSharp|Teigha)\b')
if($hits.Count){throw 'Raw/Dxf normalization/dependency leakage.'}
Push-Location -LiteralPath $repo
try{
    $diff=@(& git diff --check 2>&1|ForEach-Object{$_.ToString()});$exitCode=$LASTEXITCODE
    [ordered]@{ExitCode=$exitCode;Output=$diff;ControlledHash=$controlled;SnapshotHash=$snapshot;SceneHashes=$scene;ProductionScopeMatches=$hits.Count}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $EvidenceDirectory 'final-audit.json') -Encoding utf8
    if($exitCode -ne 0){throw 'Diff check failed.'}
}finally{Pop-Location}
'PHASE03H boundary/hash/dependency/diff audit PASS.'
