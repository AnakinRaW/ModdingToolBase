# =========================================================================================
# Dot-sourceable helpers for the per-app update-tooling config (JSON/JSONC). Parsed with
# ConvertFrom-Json, which tolerates // and /* */ comments and trailing commas.
#
#   . (Join-Path $PSScriptRoot 'AppConfig.ps1')
#   $cfg    = Import-AppConfig $ConfigPath
#   $appExe = Get-RequiredConfig $cfg 'appExe'      # throws if missing/blank
#   $root   = $cfg.RepoRoot                         # the config file's directory
#   $opt    = $cfg.Values.someOptionalField         # optional fields: read directly
# =========================================================================================

# Loads + resolves the config file. Returns an object exposing the parsed Values, the absolute
# config Path, and the RepoRoot (its directory -- all relative config paths resolve against it).
function Import-AppConfig {
    [CmdletBinding()]
    param([Parameter(Mandatory)] [string]$ConfigPath)

    if (-not (Test-Path $ConfigPath)) { throw "Config file not found at '$ConfigPath'." }
    $resolved = (Resolve-Path $ConfigPath).Path

    [pscustomobject]@{
        Path     = $resolved
        RepoRoot = Split-Path -Parent $resolved
        Values   = Get-Content -Raw $resolved | ConvertFrom-Json
    }
}

# Reads a required field from an Import-AppConfig result; throws a clear error if missing/blank.
function Get-RequiredConfig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] $Config,
        [Parameter(Mandatory)] [string]$Name
    )

    $value = $Config.Values.$Name
    if ([string]::IsNullOrWhiteSpace([string]$value)) {
        throw "Config '$($Config.Path)' is missing required field '$Name'."
    }
    return $value
}

# --- Local self-update test version pair -------------------------------------------------
# An old "installed" version and a newer "server" version, picked at the extremes so an update
# is always available. These are universal test fixtures (identical across apps), so they live
# here as a shared tooling default rather than in any app's per-app config. Dot-sourcing this
# script makes them available to the caller.
$DefaultInstalledVersion = '0.0.1-local'
$DefaultServerVersion    = '99.99.99-local'
