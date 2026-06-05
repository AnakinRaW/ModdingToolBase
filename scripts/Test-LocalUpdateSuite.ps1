# =========================================================================================
# Full self-update integration suite for a ModdingToolBase-based app: runs every scenario
# (single-channel, dual-channel, updater-mismatch) end-to-end by driving Test-LocalUpdate.ps1.
#
# This is the single entry point a CI job fires -- the job only needs to check out (with
# submodules), set up .NET, and run this script; everything else lives here. Each scenario is a
# full build + deploy + cycle and runs in its OWN pwsh process, so a scenario's `exit` can't end
# the suite and the scenarios stay isolated. Runs all scenarios (no fail-fast) and throws at the
# end if any failed, dumping the app log to aid debugging.
#
# Required config fields: those of Test-LocalUpdate.ps1, plus `appLogDir` (failure diagnostics).
# Windows-only.
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ConfigPath,
    [string]$InstalledVersion = '0.0.1-local',
    [string]$ServerVersion    = '99.99.99-local'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ConfigPath)) { throw "Config file not found at '$ConfigPath'." }
$ConfigPath = (Resolve-Path $ConfigPath).Path
$runner     = Join-Path $PSScriptRoot 'Test-LocalUpdate.ps1'

$scenarios = @(
    @{ Name = 'single';           Extra = @() },
    @{ Name = 'dual';             Extra = @('-Dual') },
    @{ Name = 'updater-mismatch'; Extra = @('-PerturbServerUpdater') }
)

$failed = @()
foreach ($s in $scenarios) {
    Write-Host "`n##################### Scenario: $($s.Name) #####################" -ForegroundColor Magenta
    $callArgs = @(
        '-NoProfile', '-File', $runner,
        '-ConfigPath',       $ConfigPath,
        '-InstalledVersion', $InstalledVersion,
        '-ServerVersion',    $ServerVersion
    ) + $s.Extra

    & pwsh @callArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Scenario '$($s.Name)' FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
        $failed += $s.Name
    } else {
        Write-Host "Scenario '$($s.Name)' passed" -ForegroundColor Green
    }
}

if ($failed.Count -gt 0) {
    # The updater log is already echoed by Test-LocalUpdateCycle; also dump the app log here.
    try {
        $appLogDir = (Get-Content -Raw $ConfigPath | ConvertFrom-Json).appLogDir
        if ($appLogDir) {
            $appLogPath = Join-Path ([Environment]::GetFolderPath('ApplicationData')) $appLogDir
            Get-ChildItem -Path $appLogPath -Filter '*.txt' -ErrorAction SilentlyContinue | ForEach-Object {
                Write-Host "`n--- $($_.FullName) ---" -ForegroundColor Yellow
                Get-Content -Raw $_.FullName | Write-Host
            }
        }
    } catch { Write-Host "(could not read app log: $_)" -ForegroundColor DarkYellow }

    throw "Integration suite failed: $($failed -join ', ')."
}

Write-Host "`nAll integration scenarios passed." -ForegroundColor Green
