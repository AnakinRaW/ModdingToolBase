# =========================================================================================
# Dot-sourceable helpers for temporarily overriding the Nerdbank.GitVersioning version
# during a local build (e.g., to stage an "installed" vs "server" version pair without
# committing version.json changes).
#
# Usage:
#   . "$repoRoot\modules\ModdingToolBase\scripts\NbgvVersion.ps1"
#
#   $nbgv = Backup-NbgvVersion -RepoRoot $repoRoot
#   try {
#       Set-NbgvVersion -Snapshot $nbgv -Version "0.0.1-local"
#       dotnet build ...
#       Set-NbgvVersion -Snapshot $nbgv -Version "99.99.99-local"
#       dotnet build ...
#   } finally {
#       Restore-NbgvVersion -Snapshot $nbgv
#   }
# =========================================================================================

function Backup-NbgvVersion {
    param([Parameter(Mandatory)] [string]$RepoRoot)
    $path = Join-Path $RepoRoot "version.json"
    if (-not (Test-Path $path)) { throw "version.json not found at '$path'." }
    [pscustomobject]@{
        Path   = $path
        Backup = [IO.File]::ReadAllText($path)
    }
}

function Restore-NbgvVersion {
    param([Parameter(Mandatory)] $Snapshot)
    [IO.File]::WriteAllText($Snapshot.Path, $Snapshot.Backup)
}

# Writes the given version into version.json. publicReleaseRefSpec is cleared so the local
# build produces a clean "X.Y.Z" InformationalVersion without the +gitHash height suffix.
function Set-NbgvVersion {
    param(
        [Parameter(Mandatory)] $Snapshot,
        [Parameter(Mandatory)] [string]$Version
    )
    $json = $Snapshot.Backup | ConvertFrom-Json
    $json.version = $Version
    if ($json.PSObject.Properties.Name -contains 'publicReleaseRefSpec') {
        $json.publicReleaseRefSpec = @()
    }
    ($json | ConvertTo-Json -Depth 32) | Set-Content -Path $Snapshot.Path -Encoding UTF8
}
