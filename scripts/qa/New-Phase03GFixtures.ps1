# Deterministic QA source generation only; never run against project assets.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$folder = Join-Path $repo 'datasets/controlled/phase03g'
[void](New-Item -ItemType Directory -Path $folder -Force)
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$newCases = [ordered]@{
    'invalid-utf8' = @([byte[]](0x31,10,0xC0,0xAF,10), 'UnsupportedEncoding', 'InvalidUtf8')
    'utf16' = @([byte[]](0xFF,0xFE,0x30,0), 'UnsupportedEncoding', 'UnsupportedBom')
    'utf32' = @([byte[]](0,0,0xFE,0xFF,0,0,0,0x30), 'UnsupportedEncoding', 'UnsupportedBom')
    'binary-dxf' = @($utf8.GetBytes("AutoCAD Binary DXF`r`n$([char]26)$([char]0)rest"), 'UnsupportedInput', 'UnsupportedInputFormat')
    'dwg-signature' = @($utf8.GetBytes('AC1032fake'), 'UnsupportedInput', 'UnsupportedInputFormat')
    'embedded-nul' = @($utf8.GetBytes("1`na$([char]0)b`n"), 'UnsupportedEncoding', 'EmbeddedNul')
    'bare-cr' = @($utf8.GetBytes("1`ra`r"), 'InvalidInput', 'InvalidLineEnding')
    'missing-value' = @($utf8.GetBytes("0`nEOF`n1`n"), 'InvalidInput', 'OddPhysicalLineCount')
    'invalid-code' = @($utf8.GetBytes("oops`na`n"), 'InvalidInput', 'InvalidGroupCode')
    'negative-code' = @($utf8.GetBytes("-1`na`n"), 'InvalidInput', 'GroupCodeOutOfRange')
    'out-of-range' = @($utf8.GetBytes("1072`na`n"), 'InvalidInput', 'GroupCodeOutOfRange')
    'noninitial-bom' = @($utf8.GetBytes("1`n$([char]0xFEFF)a`n"), 'UnsupportedEncoding', 'UnexpectedBom')
    'repeated-bom' = @($utf8.GetBytes("$([char]0xFEFF)$([char]0xFEFF)1`na`n"), 'UnsupportedEncoding', 'UnexpectedBom')
    'truncated-utf8' = @([byte[]](0x31,10,0xE2,0x82), 'UnsupportedEncoding', 'InvalidUtf8')
    'mixed-endings' = @($utf8.GetBytes("1`r`n벽체 café 😀`n0`r`nEOF`n"), 'Succeeded', '')
    'valid-bom' = @($utf8.GetBytes("$([char]0xFEFF)1`n벽체 café 😀`n"), 'Succeeded', '')
    'positive-signed-code' = @($utf8.GetBytes("+1`nraw text`n"), 'Succeeded', '')
}
foreach ($name in $newCases.Keys) { [IO.File]::WriteAllBytes((Join-Path $folder ($name + '.bin')), $newCases[$name][0]) }
# Expectations below come from hand-reviewed source/earlier fixture assertions, not parser output.
$b = @{
    'unsupported-version'=@('UnsupportedVersion','UnsupportedAcadVersion'); 'missing-version'=@('InvalidInput','MissingAcadVersion')
    'duplicate-version'=@('InvalidInput','DuplicateAcadVersion'); 'unknown-section'=@('Partial','UnknownSection')
    'nested-section'=@('InvalidInput','NestedSection'); 'missing-endsec'=@('InvalidInput','MissingEndSec')
    'missing-eof'=@('InvalidInput','MissingEof'); 'content-after-eof'=@('InvalidInput','ContentAfterEof')
    'malformed-header'=@('InvalidInput','MalformedHeaderVariable'); 'duplicate-header'=@('InvalidInput','DuplicateHeaderSection')
    'table-missing-name'=@('InvalidInput','MissingTableName'); 'nested-table'=@('InvalidInput','NestedTable')
    'missing-endtab'=@('InvalidInput','MissingEndTab'); 'unknown-table-count'=@('Partial','DeclaredTableCountMismatch')
    'dimstyle-duplicate-standard-flags'=@('Partial','InvalidTableValue'); 'dimstyle-duplicate-text-fill-color'=@('Partial','InvalidTableValue')
}
$c = @{
    'lwpolyline-count-mismatch'='VertexCountMismatch'; 'lwpolyline-missing-y'='MissingRequiredField'
    'lwpolyline-invalid-order'='InvalidVertexOrder'; 'line-missing-y'='MissingRequiredField'
    'line-nonfinite'='InvalidNumericValue'; 'line-duplicate-x'='DuplicateField'
    'unsupported-ellipse'='UnsupportedGeometry'; 'legacy-gated'='LegacyPolylineGated'
}
$d = @{
    'insert-array'=@('Partial','UnsupportedOccurrenceExpansion'); 'attributes-malformed'=@('InvalidInput','MissingAttributeSequenceEnd')
    'dimension-unsupported'=@('Partial','UnsupportedDimensionSubtype'); 'duplicate-nonfinite'=@('Partial','DuplicateField')
    'block-malformed'=@('InvalidInput','NestedBlock'); 'legacy-seqend'=@('Partial','LegacySequenceGated')
}
$e = @{
    'duplicate-same'='DuplicateHandle'; 'duplicate-cross'='DuplicateHandle'; 'duplicate-case'='DuplicateHandle'; 'duplicate-zero'='DuplicateHandle'
    'malformed'='MalformedHandle'; 'missing-required'='MissingRequiredHandle'; 'unresolved-owner'='UnresolvedOwner'
    'unresolved-pointer'='UnresolvedPointer'; 'ambiguous-target'='DuplicateHandle'; 'self-owner'='SelfOwner'
    'owner-cycle'='OwnerCycle'; 'multiple-owners'='MultipleOwners'
}
$paths = [string[]](Get-ChildItem (Join-Path $repo 'datasets/controlled') -Recurse -File | Where-Object { $_.Extension -in '.dxf','.bin' } | ForEach-Object FullName)
[Array]::Sort($paths, [StringComparer]::Ordinal)
$manifest = foreach ($path in $paths) {
    $file = Get-Item -LiteralPath $path
    $phase = $file.Directory.Name; $name = $file.BaseName; $stage = ''; $status = 'Succeeded'; $codes = @()
    switch ($phase) {
        'phase02' { $stage='03F' }
        'phase03b' { $stage='03B'; if ($b.ContainsKey($name)) { $status=$b[$name][0]; $codes=@($b[$name][1]) } }
        'phase03c' { $stage='03C'; if ($c.ContainsKey($name)) { $status='Partial'; $codes=@($c[$name]) } }
        'phase03d' { $stage='03D'; if ($d.ContainsKey($name)) { $status=$d[$name][0]; $codes=@($d[$name][1]) } }
        'phase03e' { $stage='03E'; if ($e.ContainsKey($name)) { $status='Partial'; $codes=@($e[$name]) } }
        'phase03f' { $stage='03F' }
        'phase03g' { $stage='03A'; $status=$newCases[$name][1]; if ($newCases[$name][2]) { $codes=@($newCases[$name][2]) } }
        default { throw "Unreviewed fixture folder $phase" }
    }
    $purpose = if ($phase -eq 'phase03g') { "Byte-level profile: $name" } else { "Reviewed $phase source case $name" }
    $readmePath = Join-Path $file.Directory.FullName 'README.md'
    if ($phase -ne 'phase03g' -and (Test-Path -LiteralPath $readmePath)) {
        $row = Get-Content -LiteralPath $readmePath | Where-Object { $_ -match ('^\|\s*' + [regex]::Escape($name) + '(?:\.dxf)?\s*\|') } | Select-Object -First 1
        if ($row) { $purpose = ($row -split '\|')[2].Trim() }
    }
    [ordered]@{
        FixtureId="$phase/$name"; Path=[IO.Path]::GetRelativePath($repo,$file.FullName).Replace('\','/')
        Purpose=$purpose
        ExpectedStage=$stage; ExpectedStatus=$status; ExpectedDiagnosticCodes=$codes
        ExpectedPreservationBehavior='Read-only source; exact lexical code/value spellings and physical lines on lexical acceptance; explicit refusal/diagnostics at owning stage. Narrow-stage fixture is not a promise of snapshot acceptance.'
        OriginalSHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
}
[IO.File]::WriteAllText((Join-Path $folder 'manifest.json'), ($manifest | ConvertTo-Json -Depth 5) + "`n", $utf8)
