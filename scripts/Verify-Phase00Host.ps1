param([Parameter(Mandatory)][string]$RevitInstallDir, [string]$Configuration='Release', [switch]$AttachOnly, [string]$RunName='host-ping', [ValidateSet('Dotnet','VisualStudio')][string]$Toolchain='Dotnet')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $repo "docs/status/evidence/phase00-review/$RunName"
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
& (Join-Path $PSScriptRoot 'Get-Phase00Snapshot.ps1') -OutputPath (Join-Path $evidence 'source-snapshot.json') | Out-Null
if (!$AttachOnly) {
    if(Get-Process DtoB.Desktop,Revit -ErrorAction SilentlyContinue){throw 'Close verification Desktop and Revit normally before rebuild/deployment.'}
    & (Join-Path $PSScriptRoot 'Verify-Phase00Foundation.ps1') -RevitInstallDir $RevitInstallDir -Configuration $Configuration -EvidenceDirectory (Join-Path $repo "artifacts/phase00-review/$RunName") -Toolchain $Toolchain
    & (Join-Path $PSScriptRoot 'Install-Phase00Connector.ps1') -Configuration $Configuration
    Copy-Item -LiteralPath (Join-Path $env:LOCALAPPDATA 'DtoB/phase00/connector/deployment-manifest.json') -Destination $evidence
    $hostProcess=Start-Process -FilePath (Join-Path $RevitInstallDir 'Revit.exe') -WindowStyle Hidden -PassThru
} else {
    $hosts=@(Get-Process Revit -ErrorAction SilentlyContinue)
    if ($hosts.Count -ne 1) { throw 'Attach requires exactly one running Revit process.' }
    $hostProcess=$hosts[0]
}
$deadline=[DateTime]::UtcNow.AddSeconds(60)
$logDirectory=Join-Path $env:LOCALAPPDATA 'DtoB/logs'
do {
    $log=Get-ChildItem -LiteralPath $logDirectory -Filter "revit-*-$($hostProcess.Id).log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($log -and ((Get-Content -LiteralPath $log.FullName -Raw) -match 'STARTED')) { break }
    if ($hostProcess.HasExited) { throw 'Revit exited before connector startup.' }
    Start-Sleep -Milliseconds 500
} while ([DateTime]::UtcNow -lt $deadline)
if (!$log -or (Get-Content -LiteralPath $log.FullName -Raw) -notmatch 'STARTED') { throw 'Connector did not start within 60 seconds. Observe the Revit UI; do not bypass security/authentication prompts.' }
$pingPath=Join-Path $evidence 'desktop-ping.json'
if (Test-Path -LiteralPath $pingPath) { throw 'Evidence path already exists; select a unique RunName.' }
$desktop=Join-Path $repo "apps/DtoB.Desktop/bin/$Configuration/net8.0-windows/DtoB.Desktop.exe"
$desktopProcess=Start-Process -FilePath $desktop -ArgumentList @('--verify-pid', $hostProcess.Id, '--evidence', ('"'+$pingPath+'"')) -WindowStyle Hidden -PassThru
$deadline=[DateTime]::UtcNow.AddSeconds(15)
while (!(Test-Path -LiteralPath $pingPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 200 }
if (!(Test-Path -LiteralPath $pingPath)) { throw 'Desktop evidence was not produced.' }
$result=Get-Content -LiteralPath $pingPath -Raw | ConvertFrom-Json
Copy-Item -LiteralPath $log.FullName -Destination (Join-Path $evidence 'revit-host.log')
$exe=Get-Item -LiteralPath (Join-Path $RevitInstallDir 'Revit.exe')
@{ utc=[DateTimeOffset]::UtcNow; revitPid=$hostProcess.Id; desktopPid=$desktopProcess.Id; fileVersion=$exe.VersionInfo.FileVersion; productVersion=$exe.VersionInfo.ProductVersion } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'environment.json') -Encoding utf8
if (!$result.response -or $result.response.Status -ne 0 -or $result.response.Host.ProcessId -ne $hostProcess.Id) { throw 'Actual Desktop-to-Revit PING failed; inspect evidence.' }
Write-Output "Actual host PONG verified. Revit PID=$($hostProcess.Id); Desktop PID=$($desktopProcess.Id); evidence=$evidence"
Write-Output 'Close verification Desktop and Revit normally, retain STOPPED log, then repeat with a new RunName for restart verification.'
