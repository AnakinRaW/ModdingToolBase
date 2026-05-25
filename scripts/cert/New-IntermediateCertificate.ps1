# =========================================================================================
# Issue a signing intermediate under an offline root. Fully interactive — every value is
# prompted for.
#
# Loads the root PFX with EphemeralKeySet (root key never lands in any keystore), generates
# an in-memory ECDSA P-256 intermediate keypair, signs it with the root, and exports.
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

function Read-PositiveInt {
    param([string]$Prompt)
    while ($true) {
        $raw = Read-Host $Prompt
        $n = 0
        if ([int]::TryParse($raw, [ref]$n) -and $n -gt 0) { return $n }
        Write-Host "  (positive integer required)" -ForegroundColor Yellow
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

function Confirm-Yes {
    param([string]$Prompt)
    while ($true) {
        $r = (Read-Host "$Prompt (y/n)").Trim().ToLowerInvariant()
        if ($r -eq 'y' -or $r -eq 'yes') { return $true }
        if ($r -eq 'n' -or $r -eq 'no')  { return $false }
    }
}

Write-Host "=== Intermediate signing cert generation ===`n" -ForegroundColor Cyan

$rootPfxPath = Read-ExistingFile "Root PFX path           (e.g. '.\modverify-root.pfx')"
$subject     = Read-X500Name     "Intermediate subject DN (e.g. 'CN=ModVerify Signing 2026-05')"
$outputDir   = Read-Required     "Output directory        (will be created if missing)"
$intPfxName  = Read-Required     "Intermediate PFX name   (e.g. 'modverify-int-202605.pfx')"
$months      = Read-PositiveInt  "Validity in months      (e.g. 12)"

if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir | Out-Null }
$outputDir = (Resolve-Path $outputDir).Path
$outPath = Join-Path $outputDir $intPfxName
if (Test-Path $outPath) {
    throw "Refusing to overwrite existing intermediate '$outPath'. Move or delete it first."
}

# Plaintext entry on purpose: a mistyped passphrase locks the operator out of the
# root PFX or seeds the GitHub secret with the wrong value, so the typed characters
# must be visible.
$rootPwd = Read-Host "Root PFX password"
$intPwd  = Read-Host "Intermediate PFX password (will go to GitHub Secrets)"

Write-Host "`nAbout to issue:" -ForegroundColor Cyan
Write-Host "  Root PFX:    $rootPfxPath"
Write-Host "  Subject:     $subject"
Write-Host "  Output PFX:  $outPath"
Write-Host "  Validity:    $months months"
if (-not (Confirm-Yes "Proceed?")) { Write-Host "Aborted." ; return }

$rootBytes = [IO.File]::ReadAllBytes($rootPfxPath)
$rootCert = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12(
    $rootBytes,
    $rootPwd,
    [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)

$intEcdsa = [System.Security.Cryptography.ECDsa]::Create(
    [System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
try {
    $intReq = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        $subject,
        $intEcdsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256)

    # Basic Constraints: end-entity, not a sub-CA
    $intReq.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
            $false, $false, 0, $true))

    # Key Usage: signs data (manifests), not certs
    $intReq.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,
            $true))

    # Subject Key Identifier
    $intReq.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new(
            $intReq.PublicKey, $false))

    # Sign with root
    $notBefore = [DateTimeOffset]::UtcNow.AddMinutes(-5)
    $notAfter  = [DateTimeOffset]::UtcNow.AddMonths($months)
    $serial    = [System.BitConverter]::GetBytes([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
    $intCert = $intReq.Create($rootCert, $notBefore, $notAfter, $serial)

    $intWithKey = [System.Security.Cryptography.X509Certificates.ECDsaCertificateExtensions]::CopyWithPrivateKey($intCert, $intEcdsa)
    try {
        $pfxBytes = $intWithKey.Export(
            [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $intPwd)
        [IO.File]::WriteAllBytes($outPath, $pfxBytes)
        Write-Host "`nIntermediate written to $outPath" -ForegroundColor Green
        Write-Host "  Subject:     $($intCert.Subject)"
        Write-Host "  Thumbprint:  $($intCert.Thumbprint)"
        Write-Host "  Valid until: $($intCert.NotAfter.ToString('u'))"

        Write-Host "`n--- Paste this into the GitHub Actions secret for the base64 PFX ---" -ForegroundColor Cyan
        Write-Host ([Convert]::ToBase64String($pfxBytes))
        Write-Host "--- end ---" -ForegroundColor Cyan
        Write-Host "Also set the password secret to the intermediate PFX password you just entered."
    } finally {
        $intWithKey.Dispose()
        $intCert.Dispose()
    }
} finally {
    $intEcdsa.Dispose()
    $rootCert.Dispose()
}
