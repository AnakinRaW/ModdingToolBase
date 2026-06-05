# Publishes a self-updating ModdingToolBase-based app release: builds the manifest creator /
# signer / FTP-uploader tools, stages the binaries, generates and signs the update manifest,
# and uploads to the configured target.
#
# Two upload modes via parameter sets:
#
#   FTP (default)  — production releases. Provide -SftpHost / -SftpPort / -SftpUser /
#                    -SftpPassword / -SftpBasePath.
#
#   Local          — dev/test deployments (Build-LocalRelease.ps1 etc.). Provide
#                    -LocalDeployTarget pointing at a local directory.
#
# Both modes support an optional next-generation channel (-NextOrigin paired with
# -NextSftpBasePath in FTP mode or -NextLocalDeployTarget in Local mode) for cross-API
# releases that publish to both the current and the new paths.
#
# Secrets (PFX bytes/password, SFTP user/password) are passed in as plain strings — the
# expectation is that the caller sources them from masked secret stores or generates them
# locally. The PFX is materialized to a temp file and wiped in `finally` regardless of outcome.

[CmdletBinding(DefaultParameterSetName = 'Ftp')]
param(
    # ---- Shared ----------------------------------------------------------------------------
    # Path to the main app exe to publish.
    [Parameter(Mandatory)] [string]$AppExePath,

    # Path to the build-output external updater exe. Used for ordinary deploys and for the
    # NEXT-target deploy in cross-API releases.
    [Parameter(Mandatory)] [string]$UpdaterExePath,

    # Override updater binary used for the PRIMARY deploy when the updater's calling API is
    # changing. Point at the previous-generation (CLI-compatible) binary; the next release
    # after the cutover can drop this and use the build output for both targets. Requires
    # -NextOrigin + -NextSftpBasePath/-NextLocalDeployTarget (compat updater only makes sense
    # when there is a next-gen channel for clients to migrate to).
    [string]$CompatibilityUpdaterExePath,

    # Embedded trust cert the app ships in Resources/Certs/. Verified to exist, parse as
    # X.509, and carry no private key before any deploy work begins. For local deploys this
    # is the freshly-generated dev trust cer.
    [Parameter(Mandatory)] [string]$EmbeddedTrustCertPath,

    # Absolute base URL written into the manifest's component originInfo.
    [Parameter(Mandatory)] [string]$Origin,

    # Branch ("stable", "beta", etc.) — one manifest per branch per target.
    [Parameter(Mandatory)] [string]$Branch,

    [Parameter(Mandatory)] [string]$SigningPfxBase64,
    [Parameter(Mandatory)] [string]$SigningPfxPassword,

    [string]$StagingDir = (Join-Path (Get-Location) "deploy"),
    [string]$ToolsBuildDir = (Join-Path (Get-Location) "dev"),

    # Cross-API next-generation channel origin URL. Available in both upload modes; must be
    # paired with -NextSftpBasePath (Ftp) or -NextLocalDeployTarget (Local).
    [string]$NextOrigin,

    # ---- FTP set: production SFTP upload --------------------------------------------------
    [Parameter(Mandatory, ParameterSetName = 'Ftp')] [string]$SftpHost,
    [Parameter(Mandatory, ParameterSetName = 'Ftp')] [int]$SftpPort,
    [Parameter(Mandatory, ParameterSetName = 'Ftp')] [string]$SftpUser,
    [Parameter(Mandatory, ParameterSetName = 'Ftp')] [string]$SftpPassword,
    [Parameter(Mandatory, ParameterSetName = 'Ftp')] [string]$SftpBasePath,
    [Parameter(ParameterSetName = 'Ftp')] [string]$NextSftpBasePath,

    # ---- Local set: dev/test upload to a local directory ----------------------------------
    [Parameter(Mandatory, ParameterSetName = 'Local')] [string]$LocalDeployTarget,
    [Parameter(ParameterSetName = 'Local')] [string]$NextLocalDeployTarget
)

$ErrorActionPreference = "Stop"

$isFtp = $PSCmdlet.ParameterSetName -eq 'Ftp'

# The "next" base path is mode-specific; pair it with -NextOrigin.
$nextBasePath = if ($isFtp) { $NextSftpBasePath } else { $NextLocalDeployTarget }
$nextBaseParamName = if ($isFtp) { 'NextSftpBasePath' } else { 'NextLocalDeployTarget' }
$publishNext = $NextOrigin -and $nextBasePath
if (($NextOrigin -and -not $nextBasePath) -or ($nextBasePath -and -not $NextOrigin)) {
    throw "-NextOrigin and -$nextBaseParamName must be specified together."
}

# A compat updater on the primary deploy only makes sense if there's also a next-gen channel
# to absorb the upgrade path — otherwise clients would never reach the new updater binary.
if ($CompatibilityUpdaterExePath -and -not $publishNext) {
    throw "-CompatibilityUpdaterExePath requires -NextOrigin and -$nextBaseParamName to be set."
}

# --- Locate the ModdingToolBase tool projects relative to this script ----------------------
$repoRoot = Split-Path -Parent $PSScriptRoot
$creatorProj  = Join-Path $repoRoot "src/AnakinApps/ApplicationManifestCreator/ApplicationManifestCreator.csproj"
$signerProj   = Join-Path $repoRoot "src/AnakinApps/ApplicationManifestSigner/ApplicationManifestSigner.csproj"
$uploaderProj = Join-Path $repoRoot "src/AnakinApps/FtpUploader/FtpUploader.csproj"

$creatorDll  = "AnakinRaW.ApplicationManifestCreator.dll"
$signerDll   = "AnakinRaW.ApplicationManifestSigner.dll"
$uploaderDll = "AnakinRaW.FtpUploader.dll"

$tempDir = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [IO.Path]::GetTempPath() }
$pfxPath = Join-Path $tempDir "publish-release-signing.pfx"

# Runs one stage + manifest + sign + upload cycle into a dedicated subdirectory.
function Publish-OneTarget {
    param(
        [string]$Label,
        [string]$UpdaterSource,
        [string]$TargetOrigin,
        [string]$TargetBasePath,
        [string]$TargetStagingDir
    )

    Write-Host ""
    Write-Host "=== Publishing target: $Label ===" -ForegroundColor Magenta
    Write-Host "    Origin:        $TargetOrigin"
    Write-Host "    Upload base:   $TargetBasePath"
    Write-Host "    Updater:       $UpdaterSource"

    if (Test-Path $TargetStagingDir) { Remove-Item -Recurse -Force $TargetStagingDir }
    New-Item -ItemType Directory -Path $TargetStagingDir | Out-Null

    Copy-Item -Path $AppExePath    -Destination $TargetStagingDir -Force
    Copy-Item -Path $UpdaterSource -Destination $TargetStagingDir -Force

    $stagedApp     = Join-Path $TargetStagingDir (Split-Path -Leaf $AppExePath)
    $stagedUpdater = Join-Path $TargetStagingDir (Split-Path -Leaf $UpdaterSource)

    dotnet (Join-Path $ToolsBuildDir $creatorDll) `
        -a $stagedApp `
        --appDataFiles $stagedUpdater `
        --origin $TargetOrigin `
        -o $TargetStagingDir `
        -b $Branch
    if ($LASTEXITCODE -ne 0) { throw "Manifest creation failed for target '$Label'." }

    dotnet (Join-Path $ToolsBuildDir $signerDll) `
        --manifest (Join-Path $TargetStagingDir "manifest.json") `
        --pfx $pfxPath `
        --password $SigningPfxPassword
    if ($LASTEXITCODE -ne 0) { throw "Manifest signing failed for target '$Label'." }

    if ($isFtp) {
        dotnet (Join-Path $ToolsBuildDir $uploaderDll) ftp `
            --host $SftpHost `
            --port $SftpPort `
            -u $SftpUser `
            -p $SftpPassword `
            --base $TargetBasePath `
            -s $TargetStagingDir
    } else {
        dotnet (Join-Path $ToolsBuildDir $uploaderDll) local `
            --base $TargetBasePath `
            -s $TargetStagingDir
    }
    if ($LASTEXITCODE -ne 0) { throw "Upload failed for target '$Label'." }
}

try {
    # --- 1. Verify the embedded trust cert is well-formed and public-only -----------------
    Write-Host "--- Verifying embedded trust cert ---" -ForegroundColor Cyan
    if (-not (Test-Path $EmbeddedTrustCertPath)) {
        throw "Embedded trust cert not found at '$EmbeddedTrustCertPath'."
    }
    $certBytes = [IO.File]::ReadAllBytes($EmbeddedTrustCertPath)
    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certBytes)
    if ($cert.HasPrivateKey) {
        throw "Embedded trust cert at '$EmbeddedTrustCertPath' carries a private key. Only the public .cer is allowed."
    }
    Write-Host "  Subject: $($cert.Subject)"

    # --- 2. Build the tool DLLs ---------------------------------------------------------
    Write-Host "--- Building manifest tools ---" -ForegroundColor Cyan
    dotnet build $creatorProj  --configuration Release --output $ToolsBuildDir | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Manifest creator build failed." }
    dotnet build $signerProj   --configuration Release --output $ToolsBuildDir | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Manifest signer build failed." }
    dotnet build $uploaderProj --configuration Release --output $ToolsBuildDir | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "FTP uploader build failed." }

    # --- 3. Materialize PFX for the duration of the deploys -----------------------------
    [IO.File]::WriteAllBytes($pfxPath, [Convert]::FromBase64String($SigningPfxBase64))

    # --- 4. Publish primary target (always) ---------------------------------------------
    $primaryUpdater = if ($CompatibilityUpdaterExePath) { $CompatibilityUpdaterExePath } else { $UpdaterExePath }
    $primaryBase    = if ($isFtp) { $SftpBasePath } else { $LocalDeployTarget }
    Publish-OneTarget `
        -Label "primary" `
        -UpdaterSource $primaryUpdater `
        -TargetOrigin $Origin `
        -TargetBasePath $primaryBase `
        -TargetStagingDir (Join-Path $StagingDir "primary")

    # --- 5. Publish next-generation target (cross-API release only) ----------------------
    if ($publishNext) {
        Publish-OneTarget `
            -Label "next-generation channel" `
            -UpdaterSource $UpdaterExePath `
            -TargetOrigin $NextOrigin `
            -TargetBasePath $nextBasePath `
            -TargetStagingDir (Join-Path $StagingDir "next")
    }

    Write-Host ""
    Write-Host "Release published." -ForegroundColor Green
}
finally {
    if (Test-Path $pfxPath) {
        Remove-Item -Force $pfxPath
        Write-Host "Wiped signing pfx from '$pfxPath'."
    }
}
