param([string]$RevitInstallDir, [string]$Configuration = 'Release', [string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path $repo 'artifacts/phase00-review/final' }
New-Item -ItemType Directory -Force -Path $EvidenceDirectory | Out-Null
Push-Location $repo
try {
    & dotnet build packages/DtoB.Core/DtoB.Core.csproj -c $Configuration --no-incremental -m:1 2>&1 | Out-File -Encoding utf8 -FilePath (Join-Path $EvidenceDirectory 'core-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Core build failed.' }
    $target = if ($RevitInstallDir) { 'DtoB.sln' } else { 'apps/DtoB.Desktop/DtoB.Desktop.csproj' }
    $arguments = @('build', $target, '-c', $Configuration, '--no-incremental', '-m:1')
    if ($RevitInstallDir) { $arguments += "-p:RevitInstallDir=$RevitInstallDir" }
    & dotnet @arguments 2>&1 | Out-File -Encoding utf8 -FilePath (Join-Path $EvidenceDirectory 'build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Foundation build failed.' }
    $build = Get-Content -LiteralPath (Join-Path $EvidenceDirectory 'build.log') -Raw
    $warnings = [regex]::Matches($build, 'warning (\w+)') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    if ($warnings | Where-Object { $_ -ne 'MSB3277' }) { throw 'Unexpected build warning code.' }
    if ($warnings -contains 'MSB3277') {
        $pairs = [regex]::Matches($build, 'MSB3277: "([^"]+, Version=[^"]+)"[^\r\n]*?"([^"]+, Version=[^"]+)"') | ForEach-Object { $_.Groups[1].Value + '|' + $_.Groups[2].Value } | Sort-Object -Unique
        $expected = @(
          'Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|Microsoft.VisualBasic, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a',
          'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|System.Drawing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a',
          'WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35|WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
        )
        if (!$RevitInstallDir -or (Compare-Object $expected @($pairs))) { throw 'Revit reference warning identity baseline changed.' }
    }
    & dotnet test tests/DtoB.Tests/DtoB.Tests.csproj -c $Configuration --logger 'trx;LogFileName=tests.trx' --results-directory $EvidenceDirectory 2>&1 | Out-File -Encoding utf8 -FilePath (Join-Path $EvidenceDirectory 'tests.log')
    if ($LASTEXITCODE -ne 0) { throw 'Automated tests failed.' }
    Write-Output "Foundation gate passed. Evidence: $EvidenceDirectory"
} finally { Pop-Location }

