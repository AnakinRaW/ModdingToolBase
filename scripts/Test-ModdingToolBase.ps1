# =========================================================================================
# Builds and tests the ModdingToolBase solution itself, so MTB is validated independently and
# consuming apps don't have to pull its test projects into their own solutions. This is the
# script MTB's own CI fires; an app could also fire it to gate on MTB's tests.
#
# The solution is resolved relative to this script, so it works whether run from the MTB repo
# root or from a consuming app that has MTB as a submodule. Relies on the repo's global.json
# selecting the Microsoft.Testing.Platform runner so `--report-github` is honored.
# =========================================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$solution = Join-Path (Split-Path -Parent $PSScriptRoot) 'ModdingToolBase.slnx'
if (-not (Test-Path $solution)) { throw "ModdingToolBase solution not found at '$solution'." }

dotnet test $solution --configuration $Configuration --report-github
exit $LASTEXITCODE
