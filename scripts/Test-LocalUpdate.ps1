# =========================================================================================
# App-independent bootstrap for the self-update integration test.
#
# Stages a local deploy via Build-LocalRelease.ps1, then runs the shared end-to-end cycle
# (Test-LocalUpdateCycle.ps1) against the staged install dir + signed local server. Reads the
# per-app values from a JSON config file; the app repo root is derived from the config's
# location so the script is agnostic to how the submodule is nested.
#
# Scenarios (mutually exclusive switches; omit both for the single-channel default):
#   (none)                  single-channel update cycle
#   -Dual                   also publish a next-gen /v2/ channel and test against it
#   -PerturbServerUpdater   server ships a byte-different external updater (same version) to
#                           exercise the launch-time integrity check
#
# Required config fields (JSON): toolProject, appExe, updaterExe, testUpdateBranch.
#
# Windows-only.
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ConfigPath,

    # Default to $DefaultInstalledVersion / $DefaultServerVersion (AppConfig.ps1) when omitted.
    [string]$InstalledVersion,
    [string]$ServerVersion,

    # Update channel to test. Defaults to the config's testUpdateBranch.
    [string]$Branch,

    [switch]$Dual,
    [switch]$PerturbServerUpdater,

    # Override updater binary used for the primary deploy. Requires -Dual.
    [string]$CompatibilityUpdater
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'AppConfig.ps1')

if (-not $InstalledVersion) { $InstalledVersion = $DefaultInstalledVersion }
if (-not $ServerVersion)    { $ServerVersion    = $DefaultServerVersion }

if ($CompatibilityUpdater -and -not $Dual) { throw "-CompatibilityUpdater requires -Dual." }

$cfg      = Import-AppConfig $ConfigPath
$repoRoot = $cfg.RepoRoot
$appExe   = Get-RequiredConfig $cfg 'appExe'
if (-not $Branch) { $Branch = Get-RequiredConfig $cfg 'testUpdateBranch' }

$deployScript = Join-Path $PSScriptRoot 'Build-LocalRelease.ps1'
$cycleScript  = Join-Path $PSScriptRoot 'Test-LocalUpdateCycle.ps1'

# --- 1. Stage the local deploy ---------------------------------------------------------------
$deployArgs = @{
    ConfigPath       = $cfg.Path
    InstalledVersion = $InstalledVersion
    ServerVersion    = $ServerVersion
}
if ($Dual)                  { $deployArgs.DualPublish          = $true }
if ($PerturbServerUpdater)  { $deployArgs.PerturbServerUpdater = $true }
if ($CompatibilityUpdater)  { $deployArgs.CompatibilityUpdater = $CompatibilityUpdater }

& $deployScript @deployArgs
if ($LASTEXITCODE -ne 0) { throw "Build-LocalRelease.ps1 failed (exit $LASTEXITCODE)." }

# --- 2. Resolve the server dir to test against -----------------------------------------------
# -Dual publishes both the primary channel and a next-gen /v2/ channel; test the /v2/ one.
$serverDir = if ($Dual) {
    Join-Path $repoRoot '.local_deploy\server\v2'
} else {
    Join-Path $repoRoot '.local_deploy\server'
}
if (-not (Test-Path $serverDir)) { throw "Expected server dir at '$serverDir' but it does not exist." }
$serverUri = "file:///$(((Resolve-Path $serverDir).Path -replace '\\','/'))"

# --- 3. Run the shared end-to-end cycle ------------------------------------------------------
& $cycleScript `
    -AppExePath         (Join-Path $repoRoot ".local_deploy\install\$appExe") `
    -ServerUri          $serverUri `
    -Branch             $Branch `
    -ExpectedNewVersion $ServerVersion

exit $LASTEXITCODE
