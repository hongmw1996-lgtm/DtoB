param([string]$EvidenceDirectory)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if(!$EvidenceDirectory){$EvidenceDirectory=Join-Path $repo 'artifacts/phase04/04h-20261019'}
$utf8=[Text.UTF8Encoding]::new($false,$true)
function Write-Json($name,$value){[IO.File]::WriteAllText((Join-Path $EvidenceDirectory $name),($value|ConvertTo-Json -Depth 16)+"`n",$utf8)}
function Hash($path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
Push-Location -LiteralPath $repo
try {
    $branch=& git branch --show-current; $head=& git rev-parse HEAD
    if($branch -ne 'phase/04-cad-normalization' -or $head -ne '76ff6e0d8ccac797ba60d8c5657922c796a0e8c9'){throw 'Release base/branch changed.'}
    if(@(& git diff --cached --name-only).Count){throw 'Index must remain unstaged.'}
    $before=Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'before.json') -Raw|ConvertFrom-Json
    $allowed=@('tests/DtoB.Tests/ExtractionQaCampaignTests.cs','docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md','tasks/PHASE_04.md')
    $protected=@(foreach($entry in $before){
        $after=Hash (Join-Path $repo $entry.Path)
        if($after -ne $entry.Hash -and $entry.Path -notin $allowed){throw "Pre-04H bytes changed: $($entry.Path)"}
        [ordered]@{Path=$entry.Path;Before=$entry.Hash;After=$after;Unchanged=($after -eq $entry.Hash)}
    })
    Write-Json 'protected-source-hashes.json' $protected
    $legacy=Get-Content datasets/controlled/phase03g/manifest.json -Raw|ConvertFrom-Json
    if($legacy.Count -ne 86 -or (Hash 'datasets/controlled/phase03g/manifest.json') -ne '1B3D1798CD1137C8F4933E44AB3CD8A2717DA3B3F9E7AAADC08A1E49E60B0C4B'){throw 'Legacy manifest changed.'}
    $fixtures=Get-Content datasets/controlled/phase04/manifest.json -Raw|ConvertFrom-Json
    foreach($f in $fixtures){if((Hash $f.Path) -ne $f.SHA256 -or (Get-Item -LiteralPath $f.Path).Length -ne $f.Bytes){throw "Fixture changed: $($f.Path)"}}
    $onDisk=@(Get-ChildItem datasets/controlled -File -Recurse|Where-Object {$_.Extension -in '.dxf','.bin'}|ForEach-Object {[IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/')}|Sort-Object)
    if($fixtures.Path.Count -ne @($fixtures.Path|Sort-Object -Unique).Count -or (Compare-Object $onDisk @($fixtures.Path|Sort-Object))){throw 'Corpus disk coverage mismatch.'}
    # Binary-safe Git blob audit: never route DXF through PowerShell text pipelines.
    $controlled='datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf'
    $start=[Diagnostics.ProcessStartInfo]::new('git');$start.ArgumentList.Add('cat-file');$start.ArgumentList.Add('blob');$start.ArgumentList.Add("HEAD:$controlled")
    $start.RedirectStandardOutput=$true;$start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $process=[Diagnostics.Process]::Start($start);$memory=[IO.MemoryStream]::new()
    try{$process.StandardOutput.BaseStream.CopyTo($memory);$process.WaitForExit();if($process.ExitCode){throw 'Blob read failed.'};$blob=$memory.ToArray()}finally{$process.Dispose();$memory.Dispose()}
    $source=[IO.File]::ReadAllBytes((Join-Path $repo $controlled));$sourceHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($blob))
    if($source.Length -ne 96621 -or $blob.Length -ne $source.Length -or (Hash $controlled) -ne $sourceHash -or $sourceHash -ne '2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC'){throw 'Source authority mismatch.'}
    $lineCount=[IO.File]::ReadAllLines((Join-Path $repo $controlled),$utf8).Count
    if($lineCount -ne 17474){throw 'Physical source line count changed.'}
    Write-Json 'source-authority-final.json' ([ordered]@{Path=$controlled;BlobOID=(& git rev-parse "HEAD:$controlled");SHA256=$sourceHash;Bytes=$source.Length;PhysicalLines=$lineCount;Pairs=$lineCount/2;BlobEqualsWorkingBytes=$true})
    [xml]$old=Get-Content artifacts/phase04/04g-implementation-20261016/release/tests.trx -Raw
    [xml]$final=Get-Content (Join-Path $EvidenceDirectory 'final/tests.trx') -Raw
    $baseline=@($old.TestRun.Results.UnitTestResult);$current=@($final.TestRun.Results.UnitTestResult)
    if($baseline.Count -ne 1315 -or @($current|Where-Object outcome -ne Passed).Count){throw 'Regression failed/skipped.'}
    $byId=@{};foreach($r in $current){$byId[$r.testId]=$r}
    foreach($r in $baseline){if(!$byId.ContainsKey($r.testId) -or $byId[$r.testId].testName -ne $r.testName){throw 'Baseline ID/name lost.'}}
    $new=@($current|Where-Object {$_.testId -notin $baseline.testId})
    Write-Json 'baseline-test-inventory.json' @($baseline|Select-Object testId,testName)
    Write-Json 'new-test-inventory.json' @($new|Select-Object testId,testName,outcome)
    $fixed=[ordered]@{
        'contract-invariant.json'='3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC'
        'scalar-contract-evidence.json'='568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3'
        'coordinate-evidence.json'='81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342'
        'local-transform-evidence.json'='02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5'
        'occurrence-expansion-evidence.json'='143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B'
        'effective-style-evidence.json'='7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01'
        'normalized-invariant.json'='8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24'
        'controlled-snapshot.json'='4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C'
    }
    foreach($item in $fixed.GetEnumerator()){if((Hash (Join-Path $EvidenceDirectory "final/$($item.Key)")) -ne $item.Value){throw "Accepted digest changed: $($item.Key)"}}
    foreach($culture in 'fr-FR','ko-KR'){
        if((Hash (Join-Path $EvidenceDirectory "final/contract-$culture.json")) -ne $fixed['contract-invariant.json'] -or
           (Hash (Join-Path $EvidenceDirectory "final/normalized-$culture.json")) -ne $fixed['normalized-invariant.json']){throw 'Accepted culture digest changed.'}
    }
    foreach($name in 'scene-invariant.sha256','scene-fr-FR.sha256','scene-ko-KR.sha256'){
        if((Get-Content (Join-Path $EvidenceDirectory "final/$name") -Raw).Trim() -ne '9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7'){throw '03H scene changed.'}
    }
    $comparisons=@(foreach($a in Get-ChildItem (Join-Path $EvidenceDirectory 'process-a/candidates') -File -Recurse){
        $relative=[IO.Path]::GetRelativePath((Join-Path $EvidenceDirectory 'process-a/candidates'),$a.FullName)
        $hash=Hash $a.FullName
        foreach($context in 'process-b','final'){if((Hash (Join-Path $EvidenceDirectory "$context/candidates/$relative")) -ne $hash){throw "Process mismatch: $relative"}}
        [ordered]@{Path=$relative.Replace('\','/');SHA256=$hash;Contexts=@('process-a','process-b','final')}
    })
    if($comparisons.Count -ne 186){throw 'Missing culture/process evidence.'}
    Write-Json 'process-comparison-final.json' $comparisons
    $dependencies=@(foreach($name in 'DtoB.Cad.Raw','DtoB.Cad.Dxf','DtoB.Cad','DtoB.Cad.Normalize'){
        [xml]$p=Get-Content "packages/$name/$name.csproj" -Raw
        $refs=@($p.SelectNodes('//ProjectReference')|ForEach-Object {[IO.Path]::GetFileNameWithoutExtension($_.Include)}|Sort-Object)
        $expected=switch($name){'DtoB.Cad.Raw'{@()};'DtoB.Cad.Dxf'{@('DtoB.Cad.Raw')};'DtoB.Cad'{@('DtoB.Core','DtoB.Geometry')};'DtoB.Cad.Normalize'{@('DtoB.Cad','DtoB.Cad.Raw')}}
        if($refs.Count -ne $expected.Count -or ($refs.Count -and (Compare-Object $expected $refs)) -or $p.SelectNodes('//PackageReference').Count){throw "Dependency graph changed: $name"}
        [ordered]@{Project=$name;References=$refs;Packages=0}
    })
    $protectedRoots=@('packages/DtoB.Cad.Raw','packages/DtoB.Cad.Dxf','packages/DtoB.Core','packages/DtoB.Geometry','services/DtoB.Analysis','apps/DtoB.Desktop','packages/DtoB.Bim','revit')
    if(@(& git diff --name-only HEAD -- @protectedRoots).Count -or @(& git ls-files --others --exclude-standard -- @protectedRoots).Count){throw 'Unauthorized protected production mutation.'}
    $production=@(& git ls-files --cached --others --exclude-standard -- packages/DtoB.Cad/V2 packages/DtoB.Cad.Normalize|Where-Object {$_ -like '*.cs'})
    $patterns='\b(WallCandidate|DoorCandidate|WindowCandidate|ColumnCandidate|Room|Story|RoomCandidate|StoryCandidate|BuildingElement)\b|recognition\s+confidence|drawing\s+region\s+inference|using\s+(Autodesk|System\.Windows|ACadSharp|Teigha|DtoB\.Desktop|DtoB\.Analysis|DtoB\.Bim|DtoB\.Revit)\b'
    $hits=@($production|Select-String -Pattern $patterns)
    if($hits.Count){throw 'Semantic/vendor/UI leakage; inspect matches.'}
    Write-Json 'boundary-audit.json' ([ordered]@{ProtectedRoots=$protectedRoots;ChangedProtectedPaths=0;Dependencies=$dependencies;SemanticVendorUiMatches=0;Pattern=$patterns;ProductionFiles=$production.Count;Pre04HProductionBytesUnchanged=$true})
    $core=Get-Content (Join-Path $EvidenceDirectory 'final/core-build.log') -Raw
    $full=Get-Content (Join-Path $EvidenceDirectory 'final/build.log') -Raw
    if($core -match '\b(?:warning|error) [A-Z]+\d+' -or $full -match '\berror [A-Z]+\d+'){throw 'Build diagnostic failure.'}
    $codes=@([regex]::Matches($full,'warning (\w+)')|ForEach-Object {$_.Groups[1].Value}|Sort-Object -Unique)
    if($codes.Count -ne 1 -or $codes[0] -ne 'MSB3277'){throw 'Unexpected warning codes.'}
    $pairs=@([regex]::Matches($full,'MSB3277: "([^"]+, Version=[^"]+)"[^\r\n]*?"([^"]+, Version=[^"]+)"')|ForEach-Object {$_.Groups[1].Value+'|'+$_.Groups[2].Value}|Sort-Object -Unique)
    $expected=@('Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|Microsoft.VisualBasic, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a',
        'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|System.Drawing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a',
        'WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35|WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
    if(Compare-Object $expected $pairs){throw 'Warning identities changed.'}
    & git diff --check;if($LASTEXITCODE){throw 'Tracked diff check failed.'}
    $changed=@(& git -c core.quotepath=false diff --name-only HEAD)
    $untracked=@(& git -c core.quotepath=false ls-files --others --exclude-standard)
    $paths=@(@($changed)+@($untracked)|Sort-Object -Unique)
    $excluded=@('docs/status/PHASE_03_GIT_RELEASE_REPORT.md','docs/status/PHASE_03_PR_REPORT.md','docs/status/PHASE_03_RELEASE_REPORT.md')
    $inventory=@(foreach($path in $paths){
        $category=switch -Regex ($path){
            '^spikes/'{'historical excluded artifacts';break}
            '^docs/status/PHASE_03_'{'historical excluded artifacts';break}
            '^reviews/PHASE_04'{'reviews';break}
            '^datasets/controlled/phase04/'{'fixtures';break}
            '^scripts/qa/Invoke-Phase04'{'04H';break}
            '^packages/DtoB.Cad/V2/|^packages/DtoB.Cad.Normalize/(NormalizationBoundary|DtoB.Cad.Normalize.csproj)'{'04A';break}
            '^packages/DtoB.Cad.Normalize/Scalars/'{'04B';break}
            '^packages/DtoB.Cad.Normalize/Coordinates/'{'04C';break}
            '^packages/DtoB.Cad.Normalize/Placement/'{'04D';break}
            '^packages/DtoB.Cad.Normalize/Occurrences/'{'04E';break}
            '^packages/DtoB.Cad.Normalize/Styles/'{'04F';break}
            '^packages/DtoB.Cad.Normalize/Assembly/'{'04G';break}
            '^tests/DtoB.Tests/'{'tests';break}
            '^(docs/(adr|plans|status)/.*PHASE_04|docs/adr/ADR-0(18|19|20)|docs/status/phase04/|tasks/PHASE_04.md|DtoB.sln)'{'04 architecture/docs';break}
            default {throw "Unclassified path: $path"}
        }
        $candidate=$category -ne 'historical excluded artifacts'
        if($candidate){
            $text=$utf8.GetString([IO.File]::ReadAllBytes((Join-Path $repo $path)))
            if($text -match '(?m)[\t ]+\r?$'){throw "Trailing whitespace: $path"}
        }elseif($path -notin $before.Path){throw "New historical exclusion not allowed: $path"}
        [ordered]@{Path=$path;Category=$category;CandidateReleasePayload=$candidate}
    })
    Write-Json 'whole-phase-path-inventory.json' $inventory
    $ignored=@(Get-ChildItem (Join-Path $repo 'artifacts/phase04') -File -Recurse|ForEach-Object {[IO.Path]::GetRelativePath($repo,$_.FullName).Replace('\','/')}|Sort-Object)
    Write-Json 'ignored-evidence-inventory.json' $ignored
    Write-Json 'final-audit.json' ([ordered]@{Branch=$branch;BaseHEAD=$head;Pre04HFiles=$before.Count;ChangedPre04H=@($protected|Where-Object {!$_.Unchanged}|ForEach-Object Path);
        FixtureInputs=$fixtures.Count;LegacyInputs=$legacy.Count;BaselineTests=$baseline.Count;RetainedBaselineTests=$baseline.Count;NewTests=$new.Count;Total=$current.Count;Passed=$current.Count;Failed=0;Skipped=0;
        FixedDigests=$fixed;ProcessComparisons=$comparisons.Count;CoreErrors=0;CoreWarnings=0;SolutionErrors=0;WarningAllowlist=$pairs;
        ProtectedBoundaryAudit='PASS';WholePhaseInventory='whole-phase-path-inventory.json';GitDiffCheck='PASS';StrictUtf8AndTrailingWhitespace='PASS';StagedFiles=0;
        CandidatePaths=@($inventory|Where-Object CandidateReleasePayload|ForEach-Object Path);HistoricalExcluded=@($inventory|Where-Object {!$_.CandidateReleasePayload}|ForEach-Object Path)})
    'PHASE04 source/fixture/byte/digest/process/regression/dependency/whitespace audit PASS.'
} finally {Pop-Location}
