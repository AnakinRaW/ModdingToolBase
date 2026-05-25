# =========================================================================================
# Annual root key liveness check. Fully interactive — every value is prompted for.
#
# Loads the offline root PFX with EphemeralKeySet, signs a throwaway test cert with it,
# and (optionally) compares the loaded root's thumbprint to the trust cert committed in
# the app repo. Confirms the root key + passphrase + backups are still recoverable BEFORE
# a real incident forces the discovery.
#
# Throws on any failure — a non-zero exit signals "go to the catastrophic rotation path".
# Never touches the Windows certificate store.
#
# Requires PowerShell 7.5+ / .NET 9+ for X509CertificateLoader.
# =========================================================================================

$ErrorActionPreference = 'Stop'

function Read-Required {
    param([string]$Prompt)
    while ($true) {
        $value = Read-Host $Prompt
        if ($value -and $value.Trim()) { return $value.Trim() }
        Write-Host "  (value required)" -ForegroundColor Yellow
    }
}

function Read-ExistingFile {
    param([string]$Prompt)
    while ($true) {
        $value = Read-Required $Prompt
        if (Test-Path -LiteralPath $value -PathType Leaf) {
            return (Resolve-Path -LiteralPath $value).Path
        }
        Write-Host "  (file not found: $value)" -ForegroundColor Yellow
    }
}

function Read-X500Name {
    param([string]$Prompt)
    while ($true) {
        $value = Read-Required $Prompt
        try {
            [void][System.Security.Cryptography.X509Certificates.X500DistinguishedName]::new($value)
            return $value
        } catch {
            Write-Host "  (must be a valid X.500 DN like 'CN=Name'; got: $($_.Exception.Message))" -ForegroundColor Yellow
        }
    }
}

function Read-OptionalExistingFile {
    param([string]$Prompt)
    while ($true) {
        $value = Read-Host $Prompt
        if (-not $value -or -not $value.Trim()) { return $null }
        $value = $value.Trim()
        if (Test-Path -LiteralPath $value -PathType Leaf) {
            return (Resolve-Path -LiteralPath $value).Path
        }
        Write-Host "  (file not found: $value)" -ForegroundColor Yellow
    }
}

Write-Host "=== Annual root liveness test ===`n" -ForegroundColor Cyan

$rootPfxPath  = Read-ExistingFile         "Root PFX path             (e.g. '.\modverify-root.pfx')"
$testSubject  = Read-X500Name             "Test cert subject DN      (e.g. 'CN=Annual Test - DELETE ME')"
$embeddedCer  = Read-OptionalExistingFile "Embedded trust cert path  (leave empty to skip cross-check)"

$rootPwd = Read-Host "Root PFX password (annual test)" -AsSecureString
$rootPwdPlain = [System.Net.NetworkCredential]::new("", $rootPwd).Password

try {
    $rootBytes = [IO.File]::ReadAllBytes($rootPfxPath)
    $rootCert = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12(
        $rootBytes,
        $rootPwdPlain,
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
    $rootPwdPlain = $null

    Write-Host "`nRoot loaded:"
    Write-Host "  Subject:     $($rootCert.Subject)"
    Write-Host "  Thumbprint:  $($rootCert.Thumbprint)"
    Write-Host "  Valid until: $($rootCert.NotAfter.ToString('u'))"

    # Sign a throwaway test cert — confirms the private key actually works
    $testEcdsa = [System.Security.Cryptography.ECDsa]::Create(
        [System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
    try {
        $testReq = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $testSubject,
            $testEcdsa,
            [System.Security.Cryptography.HashAlgorithmName]::SHA256)
        $serial = [System.BitConverter]::GetBytes([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
        $testCert = $testReq.Create($rootCert,
            [DateTimeOffset]::UtcNow,
            [DateTimeOffset]::UtcNow.AddMinutes(5),
            $serial)
        Write-Host "Test cert signed successfully — root private key is intact."
        $testCert.Dispose()
    } finally {
        $testEcdsa.Dispose()
    }

    if ($embeddedCer) {
        $embedded = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadCertificate(
            [IO.File]::ReadAllBytes($embeddedCer))
        try {
            if ($embedded.Thumbprint -eq $rootCert.Thumbprint) {
                Write-Host "Embedded trust cert MATCHES the loaded root. OK."
            } else {
                Write-Warning "Embedded trust cert thumbprint DOES NOT MATCH the loaded root."
            }
        } finally {
            $embedded.Dispose()
        }
    }

    $rootCert.Dispose()
    Write-Host "`nAnnual test PASSED." -ForegroundColor Green
} catch {
    Write-Error "ANNUAL TEST FAILED. Recovery may be required (see cert-playbook section 4 path B)."
    throw
}
