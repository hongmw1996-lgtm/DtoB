param([string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path $repo 'artifacts/phase03/03g-qa-20261015' }
$utf8 = [Text.UTF8Encoding]::new($false, $true)
function Write-Evidence([string]$name, $value) {
    [IO.File]::WriteAllText((Join-Path $EvidenceDirectory $name), ($value | ConvertTo-Json -Depth 12) + "`n", $utf8)
}
$before = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'before.json') -Raw | ConvertFrom-Json
$protected = foreach ($item in $before) {
    $path = Join-Path $repo $item.Path
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Baseline file missing: $($item.Path)" }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    [ordered]@{ Path=$item.Path; Before=$item.Hash; After=$hash; Unchanged=($item.Hash -eq $hash) }
}
$unexpected = @($protected | Where-Object { !$_.Unchanged -and $_.Path.Replace('\','/') -ne 'docs/status/PHASE_03_CHECKLIST.md' })
Write-Evidence 'protected-source-hashes.json' @($protected)
if ($unexpected.Count) { throw "Unexpected pre-existing source changes: $($unexpected.Path -join ', ')" }
$checklist = [IO.File]::ReadAllText((Join-Path $repo 'docs/status/PHASE_03_CHECKLIST.md'))
$checklistBefore = [IO.File]::ReadAllText((Join-Path $EvidenceDirectory 'checklist-before.md'))
if (!$checklist.StartsWith($checklistBefore, [StringComparison]::Ordinal)) { throw 'Checklist history was rewritten.' }
Write-Evidence 'checklist-prefix.json' ([ordered]@{ OriginalPrefixPreserved=$true })
$manifestPath = Join-Path $repo 'datasets/controlled/phase03g/manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$sourceHashes = foreach ($entry in $manifest) {
    $hash = (Get-FileHash -LiteralPath (Join-Path $repo $entry.Path) -Algorithm SHA256).Hash
    if ($hash -ne $entry.OriginalSHA256) { throw "Fixture changed: $($entry.Path)" }
    [ordered]@{ Path=$entry.Path; SHA256=$hash; MatchesManifest=$true }
}
Write-Evidence 'fixture-source-hashes.json' @($sourceHashes)
Write-Evidence 'manifest-hash.json' ([ordered]@{ SHA256=(Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash; Entries=$manifest.Count })
$controlled = $manifest | Where-Object FixtureId -eq 'phase02/DtoB_TEST_2STORY_R2018'
if ($controlled.OriginalSHA256 -ne '2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC') { throw 'Controlled source identity mismatch.' }

$audits = foreach ($name in 'DtoB.Cad.Raw','DtoB.Cad.Dxf') {
    $folder = Join-Path $repo "packages/$name"
    [xml]$project = Get-Content -LiteralPath (Join-Path $folder "$name.csproj") -Raw
    $references = @($project.SelectNodes('//ProjectReference') | ForEach-Object { $_.Include.Replace('\','/') })
    $packages = @($project.SelectNodes('//PackageReference') | ForEach-Object Include)
    if ($packages.Count) { throw "Unexpected package dependency in $name" }
    if ($name -eq 'DtoB.Cad.Raw' -and $references.Count) { throw 'Raw must remain runtime-only.' }
    if ($name -eq 'DtoB.Cad.Dxf' -and ($references.Count -ne 1 -or $references[0] -ne '../DtoB.Cad.Raw/DtoB.Cad.Raw.csproj')) { throw 'Dxf dependency boundary changed.' }
    [ordered]@{ Project=$name; ProjectReferences=$references; PackageReferences=$packages }
}
Write-Evidence 'dependency-audit.json' @($audits)
$patterns = [ordered]@{
    'VendorOrUiImport'='(?m)^\s*using\s+(ACadSharp|ODA|Teigha|Autodesk|netDxf|System\.Windows|DtoB\.Project|DtoB\.Core|DtoB\.Geometry)\b'
    'TransformOrNormalizationCall'='Math\.(Sin|Cos|Tan|Atan2)\s*\(|UnitUtils\.|ConvertToInternalUnits\s*\(|Matrix3D|ArbitraryAxisAlgorithm|Normalize\s*\('
    'SourceFileWriteOrPathIo'='File\.(Write|Read|Open)|Directory\.|new\s+(FileStream|StreamWriter)\s*\('
}
$sources = @(Get-ChildItem (Join-Path $repo 'packages/DtoB.Cad.Raw'),(Join-Path $repo 'packages/DtoB.Cad.Dxf') -File -Filter '*.cs')
$scope = foreach ($entry in $patterns.GetEnumerator()) {
    $hits = @($sources | Select-String -Pattern $entry.Value)
    if ($hits.Count) { throw "Scope audit requires investigation: $($entry.Key): $($hits -join ', ')" }
    [ordered]@{ Check=$entry.Key; Pattern=$entry.Value; Matches=0; Files=$sources.Count; Meaning='Targeted static guard; complemented by manual production-source review and unchanged baseline hashes.' }
}
Write-Evidence 'scope-audit.json' @($scope)
$priorPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($item in $before) { [void]$priorPaths.Add($item.Path.Replace('\','/')) }
Push-Location -LiteralPath $repo
try { $newFiles = @(& rg --files | Where-Object { !$priorPaths.Contains($_.Replace('\','/')) }) }
finally { Pop-Location }
$newText = foreach ($relative in $newFiles) {
    if ([IO.Path]::GetExtension($relative) -eq '.bin') { continue } # Deliberately invalid byte fixtures.
    $path = Join-Path $repo $relative; $text = $utf8.GetString([IO.File]::ReadAllBytes($path))
    if ($text -match '(?m)[\t ]+\r?$') { throw "New text has trailing whitespace: $relative" }
    [ordered]@{ Path=$relative; StrictUTF8=$true; NoTrailingWhitespace=$true; SHA256=(Get-FileHash -LiteralPath $path).Hash }
}
Write-Evidence 'new-text-review.json' @($newText)

# Separate testhost processes avoid global CurrentDirectory mutations in parallel xUnit tests.
$vstest = 'C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe'
$dll = Join-Path $repo 'tests/DtoB.Tests/bin/Release/net8.0/DtoB.Tests.dll'
$oldEvidence = $env:DTOB_03G_EVIDENCE_DIRECTORY
try {
    foreach ($name in 'workdir-alpha','workdir-beta') {
        $working = Join-Path ([IO.Path]::GetTempPath()) "DtoB-03G-$name"
        [void](New-Item -ItemType Directory -Path $working -Force)
        $resultPath = Join-Path $EvidenceDirectory $name
        [void](New-Item -ItemType Directory -Path $resultPath -Force)
        $env:DTOB_03G_EVIDENCE_DIRECTORY = $resultPath
        Push-Location -LiteralPath $working
        try {
            & $vstest $dll '/TestCaseFilter:FullyQualifiedName~FreshFullPipelinesHaveIdenticalOrderAndAuthoritativeHashAcrossCultures' '/Logger:trx;LogFileName=tests.trx' "/ResultsDirectory:$resultPath" *> (Join-Path $resultPath 'tests.log')
            if ($LASTEXITCODE -ne 0) { throw "Determinism process failed in $working" }
        } finally { Pop-Location }
        $snapshotHash = (Get-FileHash -LiteralPath (Join-Path $resultPath 'controlled-snapshot.json') -Algorithm SHA256).Hash
        if ($snapshotHash -ne '4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C') { throw 'Working-directory determinism hash changed.' }
    }
} finally { $env:DTOB_03G_EVIDENCE_DIRECTORY = $oldEvidence }
Push-Location -LiteralPath $repo
try {
    $diff = @(& git diff --check 2>&1 | ForEach-Object { $_.ToString() }); $code = $LASTEXITCODE
    Write-Evidence 'git-diff-check.json' ([ordered]@{ ExitCode=$code; Output=$diff })
    if ($code -ne 0) { throw 'git diff --check failed.' }
} finally { Pop-Location }
'PHASE03G source/dependency/scope/different-working-directory audit PASS.'
