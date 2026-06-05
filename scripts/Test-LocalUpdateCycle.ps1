# =========================================================================================
# App-independent end-to-end test for the self-update flow of a ModdingToolBase-based app.
#
# Drives a full cycle against a pre-staged install dir + signed local server:
#   1. Launch the installed exe with the `updateApplication` verb pointing at the local
#      server. Assert the host exits with the restart-required code.
#   2. Wait for the AnakinRaW.ExternalUpdater grandchild to apply the update.
#   3. Assert the on-disk exe is now the expected new version and the updater log reports
#      no errors.
#   4. Assert the updater relaunched the freshly-installed exe with
#      `--externalUpdaterResult UpdateSuccess` (evidence: updater's own log).
#   5. Re-invoke `updateApplication` on the now-updated install and assert it's a clean no-op:
#      it exits 0 (no RestartRequiredCode) and leaves the on-disk exe version unchanged.
#
# The caller is responsible for staging the install dir + signed local server first
# (typically via `Build-LocalRelease.ps1`, which wraps `Publish-LocalRelease.ps1`).
#
# USAGE (from an app's CI script or wrapper)
#
#   Test-LocalUpdateCycle.ps1 -AppExePath <path>        `
#                             -ServerUri <uri>          `
#                             -Branch <name>            `
#                             [-ExpectedNewVersion <ver>]
#
# Windows-only (the external updater is a Windows-only binary; the net481 self-updating
# build is also Windows-only).
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    # Absolute path to the staged install copy of the app exe.
    [Parameter(Mandatory)] [string]$AppExePath,

    # Base URI of the local signed update server (typically a file:/// URI produced by
    # Publish-LocalRelease.ps1; manifests are resolved as <ServerUri>/<Branch>/manifest.json).
    [Parameter(Mandatory)] [string]$ServerUri,

    # Branch name to update against; must match the one passed to Publish-LocalRelease.ps1.
    [Parameter(Mandatory)] [string]$Branch,

    # When provided, the post-update ProductVersion must start with this string. Useful when
    # the caller knows the server-side version it just published. When omitted the test only
    # asserts the version changed.
    [string]$ExpectedNewVersion,

    # Upper bound for the external updater to apply the update + relaunch the target.
    [int]$UpdaterTimeoutSec = 120,

    # Polling window after the updater process exits, while the relaunched target finishes
    # initializing and the updater's log gets its final lines flushed.
    [int]$RelaunchWaitSec   = 30
)

$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw "Test-LocalUpdateCycle.ps1 requires Windows (net481 + external updater)."
}

if (-not (Test-Path $AppExePath)) {
    throw "AppExePath '$AppExePath' does not exist. Stage the install dir before running."
}
$AppExePath = (Resolve-Path $AppExePath).Path

# Framework-controlled constants (defined in ModdingToolBase).
$updaterProcessName = 'AnakinRaW.ExternalUpdater'  # ExternalUpdater.App project name
# Serilog.Extensions.Logging.File rolls daily, so the real file is `extUpdateLog-YYYYMMDD.txt`.
$updaterLogDir      = $env:TEMP                    # LoggingDirectory = Path.GetTempPath()
$updaterLogGlob     = 'extUpdateLog*.txt'
$restartRequiredCode = 3010  # RestartConstants.RestartRequiredCode

function Get-UpdaterLogFile {
    Get-ChildItem -Path $updaterLogDir -Filter $updaterLogGlob -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        Write-Host "  FAIL: $Message" -ForegroundColor Red
        throw $Message
    }
    Write-Host "  PASS: $Message" -ForegroundColor Green
}

# ---- Pre-flight clean-up so observations are scoped to this cycle -----------------------
Get-Process -Name $updaterProcessName -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Killing leftover updater process (PID $($_.Id))" -ForegroundColor Yellow
    $_ | Stop-Process -Force -ErrorAction SilentlyContinue
}
Get-ChildItem -Path $updaterLogDir -Filter $updaterLogGlob -File -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

$preVersion = (Get-Item $AppExePath).VersionInfo.ProductVersion
Write-Host "Pre-update ProductVersion: $preVersion"

# =========================================================================================
Write-Host "`n=== [1/5] Launch update cycle ===" -ForegroundColor Cyan
$updateArgs = @(
    'updateApplication',
    '--updateBranch',    $Branch,
    '--updateServerUrl', $ServerUri,
    '--verboseUpdateLogging'
)
# Start-Process -NoNewWindow keeps the child's standard handles attached to the real
# Win32 console. Required for console-using apps: piping (`& exe | Out-Host`) wraps the
# child's stdout in a non-console handle, which breaks any code that touches the cursor
# (e.g., a spinner that reads Console.CursorVisible -> 'The handle is invalid.').
$proc = Start-Process -FilePath $AppExePath -ArgumentList $updateArgs `
    -NoNewWindow -Wait -PassThru
$updateExit = $proc.ExitCode
Assert-True ($updateExit -eq $restartRequiredCode) `
    "Host exited with RestartRequiredCode $restartRequiredCode (got $updateExit)"

# =========================================================================================
Write-Host "`n=== [2/5] Wait for external updater to finish (max ${UpdaterTimeoutSec}s) ===" -ForegroundColor Cyan
# Start-Process -Wait waits for descendants too, so the updater grandchild may already
# be gone by this point. We don't try to catch the process live (that's racy); the
# version change on disk is the definitive signal that the updater ran and finished.
$deadline    = (Get-Date).AddSeconds($UpdaterTimeoutSec)
$postVersion = $preVersion

while ((Get-Date) -lt $deadline) {
    $item = Get-Item $AppExePath -ErrorAction SilentlyContinue
    if ($item) { $postVersion = $item.VersionInfo.ProductVersion }
    if ($postVersion -and ($postVersion -ne $preVersion)) { break }
    Start-Sleep -Milliseconds 500
}

Assert-True ($postVersion -ne $preVersion) `
    "Installed exe was replaced (was: $preVersion, now: $postVersion)"
if ($ExpectedNewVersion) {
    # Compare on MAJOR.MINOR.PATCH only. Nbgv may rewrite the prerelease tag and append
    # +commitHash even when the caller passes a clean SemVer (e.g., '99.99.99-local'
    # becomes '99.99.99-beta+abc1234'). The numeric prefix is what we actually control.
    $expectedNumeric = [regex]::Match($ExpectedNewVersion, '^\d+\.\d+\.\d+').Value
    $postNumeric     = [regex]::Match($postVersion,       '^\d+\.\d+\.\d+').Value
    Assert-True ($expectedNumeric -and $postNumeric -and ($expectedNumeric -eq $postNumeric)) `
        "Post-update version '$postVersion' has MAJOR.MINOR.PATCH '$postNumeric', expected '$expectedNumeric' (from '$ExpectedNewVersion')"
}

# =========================================================================================
Write-Host "`n=== [3/5] Inspect external-updater log ===" -ForegroundColor Cyan
$updaterLogFile = Get-UpdaterLogFile
Assert-True ($null -ne $updaterLogFile) "Updater log appeared under $updaterLogDir ($updaterLogGlob)"
$logText = Get-Content -Raw $updaterLogFile.FullName
Write-Host "--- $($updaterLogFile.Name) ---"
Write-Host $logText
Write-Host "--- end log ---"
Assert-True ($logText -notmatch '\[FTL\]|\[ERR\]') "Updater log contains no FTL/ERR entries"

# =========================================================================================
Write-Host "`n=== [4/5] Verify target was relaunched ===" -ForegroundColor Cyan
# ProcessTools.StartApplication logs `Starting application '{Name}' with {Args}` at Info.
# Serilog wraps string placeholders in quotes, so the rendered text is
# `Starting application '"<path>"' with "<args>"`. Match the inner quotes as optional
# to stay robust against future template tweaks.
$relaunchPattern  = "Starting application '`"?" + [regex]::Escape($AppExePath) + "`"?'"
$resultArgPattern = 'externalUpdaterResult\s+UpdateSuccess'
$relaunchMatched  = $false
$resultArgMatched = $false
$relaunchDeadline = (Get-Date).AddSeconds($RelaunchWaitSec)

while ((Get-Date) -lt $relaunchDeadline) {
    $updaterLogFile = Get-UpdaterLogFile
    if ($updaterLogFile) {
        $logText = Get-Content -Raw $updaterLogFile.FullName -ErrorAction SilentlyContinue
        if ($logText -match $relaunchPattern)  { $relaunchMatched  = $true }
        if ($logText -match $resultArgPattern) { $resultArgMatched = $true }
        if ($relaunchMatched -and $resultArgMatched) { break }
    }
    Start-Sleep -Milliseconds 500
}
Assert-True $relaunchMatched  "Updater log records starting '$AppExePath' (target was relaunched)"
Assert-True $resultArgMatched "Relaunch arguments include 'externalUpdaterResult UpdateSuccess'"

# =========================================================================================
Write-Host "`n=== [5/5] Re-run updateApplication; expect a clean no-op (no second update) ===" -ForegroundColor Cyan
# Wording-independent assertion: re-running against the same server must find nothing to do. The
# framework signals "update applied" by force-exiting with RestartRequiredCode ($restartRequiredCode)
# and replacing the on-disk exe; "nothing to do" returns 0 and leaves the exe untouched. We assert
# both, rather than scraping a host-specific "no update" string from stdout (each app words it
# differently). The child's stdout still streams to the console for debugging.
$recheckProc = Start-Process -FilePath $AppExePath `
    -ArgumentList @('updateApplication', '--updateBranch', $Branch, '--updateServerUrl', $ServerUri) `
    -NoNewWindow -Wait -PassThru
$recheckExit = $recheckProc.ExitCode
Assert-True ($recheckExit -eq 0) `
    "Re-check exited 0 -- no RestartRequiredCode ($restartRequiredCode) raised (got $recheckExit)"

$recheckVersion = (Get-Item $AppExePath).VersionInfo.ProductVersion
Assert-True ($recheckVersion -eq $postVersion) `
    "Installed exe unchanged on re-check (still $postVersion) -- no second update applied"

Write-Host "`n=== ALL ASSERTIONS PASSED ===" -ForegroundColor Green
exit 0
