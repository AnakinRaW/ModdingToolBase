# =========================================================================================
# App-independent local deployment for ModdingToolBase-based apps.
#
# Reads the per-app values (project, exe names, update channel) from a JSON config file, builds
# the app twice (an older "installed" version + a newer "server" version), then hands off to the
# shared Publish-LocalRelease.ps1 for cert generation, manifest signing and install-dir staging.
# The result is staged under "<repo>/.local_deploy" so the self-update flow can be exercised
# end-to-end without hitting the real CDN.
#
# The app repo root is derived from the location of the config file, so the same script works
# regardless of how deeply the ModdingToolBase submodule is nested in the consuming repo.
#
# USAGE (from the app repo, with a config file next to it)
#   <submodule>/scripts/Build-LocalRelease.ps1 -ConfigPath ./update-tooling.jsonc
#   <submodule>/scripts/Build-LocalRelease.ps1 -ConfigPath ./update-tooling.jsonc -DualPublish
#   <submodule>/scripts/Build-LocalRelease.ps1 -ConfigPath ./update-tooling.jsonc -DualPublish -CompatibilityUpdater <path>
#
#   -CompatibilityUpdater requires -DualPublish.
#
# Required config fields (JSON): toolProject, appExe, updaterExe, testUpdateBranch.
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    # Path to the per-app JSON config. Its directory is treated as the app repo root and all
    # config paths are resolved relative to it.
    [Parameter(Mandatory)] [string]$ConfigPath,

    # Default to $DefaultInstalledVersion / $DefaultServerVersion (AppConfig.ps1) when omitted.
    [string]$InstalledVersion,
    [string]$ServerVersion,

    # Target framework of the self-updating build. net481 is the only updatable TFM (the external
    # updater is a net481-only Windows binary), so this is effectively fixed and lives here as a
    # tooling default rather than in each app's config -- exposed only for rare overrides.
    [string]$TargetFramework  = "net481",

    [switch]$DualPublish,

    # Override updater binary used for the primary deploy. Requires -DualPublish.
    [string]$CompatibilityUpdater,

    # Make the server's updater byte-different from the installed app's embedded copy (same
    # version, still runnable) so the cycle exercises the launch-time integrity check.
    [switch]$PerturbServerUpdater
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "AppConfig.ps1")
. (Join-Path $PSScriptRoot "NbgvVersion.ps1")

if (-not $InstalledVersion) { $InstalledVersion = $DefaultInstalledVersion }
if (-not $ServerVersion)    { $ServerVersion    = $DefaultServerVersion }

$cfg        = Import-AppConfig $ConfigPath
$repoRoot   = $cfg.RepoRoot
$appExe     = Get-RequiredConfig $cfg 'appExe'
$updaterExe = Get-RequiredConfig $cfg 'updaterExe'
$branch     = Get-RequiredConfig $cfg 'testUpdateBranch'
$toolProj   = Join-Path $repoRoot (Get-RequiredConfig $cfg 'toolProject')

$baseScript      = Join-Path $PSScriptRoot "Publish-LocalRelease.ps1"
$deployRoot      = Join-Path $repoRoot ".local_deploy"
$installBuildDir = Join-Path $deployRoot "bin\install"
$serverBuildDir  = Join-Path $deployRoot "bin\tool"

# Builds the app at a given version into a given output dir (uses $toolProj / $TargetFramework /
# $appExe / $nbgv from script scope).
function Invoke-AppBuild {
    param([string]$Version, [string]$OutputDir, [string]$Label)

    Write-Host "--- Building $appExe ($TargetFramework) @ $Label v$Version ---" -ForegroundColor Cyan
    Set-NbgvVersion -Snapshot $nbgv -Version $Version
    dotnet build $toolProj --configuration Release -f $TargetFramework --output $OutputDir `
        /p:DebugType=None /p:DebugSymbols=false /p:LocalDeploy=true
    if ($LASTEXITCODE -ne 0) { throw "Build of '$toolProj' @ v$Version failed." }
}

if (Test-Path $deployRoot) { Remove-Item -Recurse -Force $deployRoot }
New-Item -ItemType Directory -Path $deployRoot | Out-Null

$nbgv = Backup-NbgvVersion -RepoRoot $repoRoot
try {
    Invoke-AppBuild -Version $InstalledVersion -OutputDir $installBuildDir -Label 'installed'
    Invoke-AppBuild -Version $ServerVersion    -OutputDir $serverBuildDir  -Label 'server'

    if ($PerturbServerUpdater) {
        # Make the server's updater differ byte-for-byte from the installed app's embedded copy.
        # Deterministic builds otherwise produce identical updaters, so the cycle never exercises an
        # updater whose bytes changed. Appended trailing bytes change the SHA-256 but are ignored by
        # the PE loader (the exe still runs) and leave the version untouched — i.e. a same-version
        # rebuild, which must be trusted from the signed manifest at launch.
        $serverUpdater = Join-Path $serverBuildDir $updaterExe
        if (-not (Test-Path $serverUpdater)) { throw "Server external updater not found at '$serverUpdater'." }
        Write-Host "--- Perturbing server external updater (trailing bytes) to force a hash mismatch ---" -ForegroundColor Cyan
        $marker = [Text.Encoding]::ASCII.GetBytes("/*INTEGRITY-REGRESSION*/")
        $fs = [IO.File]::Open($serverUpdater, [IO.FileMode]::Append, [IO.FileAccess]::Write)
        try { $fs.Write($marker, 0, $marker.Length) } finally { $fs.Dispose() }
    }

    $publishParams = @{
        AppExePath      = Join-Path $serverBuildDir $appExe
        UpdaterExePath  = Join-Path $serverBuildDir $updaterExe
        DeployRoot      = $deployRoot
        InstallBuildDir = $installBuildDir
        Branch          = $branch
    }
    if ($DualPublish)          { $publishParams.DualPublish          = $true }
    if ($CompatibilityUpdater) { $publishParams.CompatibilityUpdater = $CompatibilityUpdater }

    & $baseScript @publishParams
}
finally {
    Restore-NbgvVersion -Snapshot $nbgv
}
