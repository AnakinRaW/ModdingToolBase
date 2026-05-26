# =========================================================================================
# App-independent local-release publisher for ModdingToolBase-based apps.
#
# Generates a throwaway dev signing cert (in-memory ECDSA P-256), then defers to
# Publish-Release.ps1 in local-mode. Optionally lays down an "installed" copy alongside
# the local server so a test client can be pointed at it.
#
# USAGE (called from an app's deploy-local script, after the app has built itself)
#
#   Publish-LocalRelease.ps1 -AppExePath <path> -UpdaterExePath <path>
#                            -DeployRoot <dir>  [-InstallBuildDir <dir>]
#                            [-Branch <name>]   [-DualPublish]
#                            [-CompatibilityUpdater <path>]
#
# Outputs (under -DeployRoot):
#   server\           — signed release the test client fetches from
#   install\          — copy of -InstallBuildDir (skipped if not provided)
#   server\v2\        — only when -DualPublish; next-gen channel
#   dev-trust.cer     — public cert this run's release was signed against
#   dev-signing.pfx   — private key (dev only; regenerated next run)
#
# Constraints:
#   - -CompatibilityUpdater requires -DualPublish.
#
# Never touches the Windows certificate store.
# =========================================================================================

param(
    # Path to the freshly-built app exe to publish.
    [Parameter(Mandatory)] [string]$AppExePath,

    # Path to the freshly-built external-updater exe to publish.
    [Parameter(Mandatory)] [string]$UpdaterExePath,

    # Root for staging/server/install dirs. Sub-dirs are wiped and recreated; other contents
    # of this directory are left alone (so callers can stage build outputs under it).
    [Parameter(Mandatory)] [string]$DeployRoot,

    # Build output to lay down as the "already installed" copy. Optional — when omitted, the
    # install dir is not created and the caller is responsible for choosing where the test
    # client runs from.
    [string]$InstallBuildDir,

    [string]$Branch = "beta",

    [switch]$DualPublish,

    # Override updater binary used for the primary deploy. Requires -DualPublish.
    [string]$CompatibilityUpdater
)

$ErrorActionPreference = "Stop"

if ($CompatibilityUpdater -and -not $DualPublish) {
    throw "-CompatibilityUpdater requires -DualPublish."
}
if ($CompatibilityUpdater -and -not (Test-Path $CompatibilityUpdater)) {
    throw "-CompatibilityUpdater file not found at '$CompatibilityUpdater'."
}
if ($InstallBuildDir -and -not (Test-Path $InstallBuildDir)) {
    throw "-InstallBuildDir not found at '$InstallBuildDir'."
}

$publishScript = Join-Path $PSScriptRoot "Publish-Release.ps1"

$serverDir     = Join-Path $DeployRoot "server"
$nextServerDir = Join-Path $DeployRoot "server\v2"
$installDir    = Join-Path $DeployRoot "install"
$stagingDir    = Join-Path $DeployRoot "staging"
$toolsDir      = Join-Path $DeployRoot "bin\tools"

$devPfx = Join-Path $DeployRoot "dev-signing.pfx"
$devCer = Join-Path $DeployRoot "dev-trust.cer"
$devPwd = "devpass"

if (-not (Test-Path $DeployRoot)) { New-Item -ItemType Directory -Path $DeployRoot | Out-Null }

# Reset only the dirs we own — leave anything else under $DeployRoot (build outputs etc.)
# alone.
foreach ($d in @($serverDir, $stagingDir, $toolsDir)) {
    if (Test-Path $d) { Remove-Item -Recurse -Force $d }
    New-Item -ItemType Directory -Path $d | Out-Null
}
if ($DualPublish) {
    if (Test-Path $nextServerDir) { Remove-Item -Recurse -Force $nextServerDir }
    New-Item -ItemType Directory -Path $nextServerDir | Out-Null
}
if ($InstallBuildDir) {
    if (Test-Path $installDir) { Remove-Item -Recurse -Force $installDir }
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

# Generate a throwaway dev signing cert (in-memory only)
Write-Host "--- Generating dev signing cert ---" -ForegroundColor Cyan
$curve = [System.Security.Cryptography.ECCurve]::CreateFromFriendlyName("nistP256")
$ecdsa = [System.Security.Cryptography.ECDsa]::Create($curve)
$req = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
    "CN=ModdingToolBase Dev Signing",
    $ecdsa,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256)
$cert = $req.CreateSelfSigned(
    [DateTimeOffset]::UtcNow.AddDays(-1),
    [DateTimeOffset]::UtcNow.AddYears(10))
[IO.File]::WriteAllBytes($devPfx, $cert.Export(
    [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $devPwd))
[IO.File]::WriteAllBytes($devCer, $cert.Export(
    [System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
$cert.Dispose()
$ecdsa.Dispose()

# Hand off to the shared publish script (local mode)
$serverUri = "file:///$((Resolve-Path $serverDir).Path.Replace('\', '/'))"
$devPfxB64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($devPfx))

$publishParams = @{
    AppExePath            = $AppExePath
    UpdaterExePath        = $UpdaterExePath
    EmbeddedTrustCertPath = $devCer
    Origin                = $serverUri
    Branch                = $Branch
    SigningPfxBase64      = $devPfxB64
    SigningPfxPassword    = $devPwd
    LocalDeployTarget     = $serverDir
    StagingDir            = $stagingDir
    ToolsBuildDir         = $toolsDir
}
if ($CompatibilityUpdater) {
    $publishParams.CompatibilityUpdaterExePath = $CompatibilityUpdater
}
if ($DualPublish) {
    $publishParams.NextOrigin            = "file:///$((Resolve-Path $nextServerDir).Path.Replace('\', '/'))"
    $publishParams.NextLocalDeployTarget = $nextServerDir
}
& $publishScript @publishParams

if ($InstallBuildDir) {
    Write-Host "--- Staging install dir from $InstallBuildDir ---" -ForegroundColor Cyan
    Copy-Item "$InstallBuildDir\*" $installDir -Recurse
}

$appExeName = Split-Path -Leaf $AppExePath

Write-Host "`nLocal release published." -ForegroundColor Green
Write-Host "Server URI:         $serverUri"
Write-Host "Server directory:   $serverDir"
if ($DualPublish)      { Write-Host "Next-gen channel:   $nextServerDir" }
if ($InstallBuildDir)  { Write-Host "Install directory:  $installDir" }
Write-Host ""
Write-Host "Run the test client against the local server:"
if ($InstallBuildDir) {
    Write-Host "  cd '$installDir'"
    Write-Host "  .\$appExeName updateApplication --updateBranch $Branch --updateServerUrl '$serverUri'"
} else {
    Write-Host "  <your app> updateApplication --updateBranch $Branch --updateServerUrl '$serverUri'"
}
