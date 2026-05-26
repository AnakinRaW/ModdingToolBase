# =========================================================================================
# Generate a self-signed offline root CA. Fully interactive — every value is prompted for.
#
# Generates an in-memory ECDSA P-256 keypair and exports directly to PFX/CER.
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

Write-Host "=== Root CA generation ===`n" -ForegroundColor Cyan

$subject    = Read-X500Name    "Subject DN        (e.g. 'CN=ModVerify Root CA')"
$outputBase = Read-Required    "Output path base  (e.g. '.\modverify-root' — script appends .pfx and .cer)"
$years      = Read-PositiveInt "Validity in years (e.g. 20)"

# Tolerate the user typing a .pfx/.cer suffix; we'll add the correct extension.
$outputBase = $outputBase -replace '\.(pfx|cer|crt)$', ''

$baseDir  = [IO.Path]::GetDirectoryName($outputBase)
$baseName = [IO.Path]::GetFileName($outputBase)
if (-not $baseName) { throw "Output path base must include a filename, got: $outputBase" }
if (-not $baseDir)  { $baseDir = '.' }
if (-not (Test-Path -LiteralPath $baseDir)) { New-Item -ItemType Directory -Path $baseDir | Out-Null }
$baseDir = (Resolve-Path -LiteralPath $baseDir).Path

$rootPfx  = Join-Path $baseDir "$baseName.pfx"
$trustCer = Join-Path $baseDir "$baseName.cer"

if ((Test-Path -LiteralPath $rootPfx) -or (Test-Path -LiteralPath $trustCer)) {
    throw "Refusing to overwrite existing '$baseName.pfx' or '$baseName.cer' in '$baseDir'. Move or delete them first."
}

# Plaintext entry on purpose: a mistyped root passphrase is unrecoverable, so the
# operator must be able to read what they're typing.
$pwd = Read-Host "Root PFX password (write this down — losing it = losing the root)"

Write-Host "`nAbout to generate:" -ForegroundColor Cyan
Write-Host "  Subject:    $subject"
Write-Host "  PFX out:    $rootPfx"
Write-Host "  CER out:    $trustCer"
Write-Host "  Validity:   $years years"
if (-not (Confirm-Yes "Proceed?")) { Write-Host "Aborted." ; return }

$ecdsa = [System.Security.Cryptography.ECDsa]::Create(
    [System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
try {
    $req = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        $subject,
        $ecdsa,
        [System.Security.Cryptography.HashAlgorithmName]::SHA256)

    # Basic Constraints: CA=true, end-entity intermediates not sub-CAs
    $req.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
            $true, $true, 0, $true))

    # Key Usage: signs other certs
    $req.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
            [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign,
            $true))

    # Subject Key Identifier — helps X509Chain link intermediates to this root
    $req.CertificateExtensions.Add(
        [System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension]::new(
            $req.PublicKey, $false))

    $notBefore = [DateTimeOffset]::UtcNow.AddMinutes(-5)
    $notAfter  = [DateTimeOffset]::UtcNow.AddYears($years)
    $cert = $req.CreateSelfSigned($notBefore, $notAfter)
    try {
        [IO.File]::WriteAllBytes($rootPfx, $cert.Export(
            [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $pwd))
        [IO.File]::WriteAllBytes($trustCer, $cert.Export(
            [System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
        Write-Host "`nRoot generated." -ForegroundColor Green
        Write-Host "  PFX:         $rootPfx"
        Write-Host "  Trust cert:  $trustCer"
        Write-Host "  Subject:     $($cert.Subject)"
        Write-Host "  Thumbprint:  $($cert.Thumbprint)"
        Write-Host "  Valid until: $($cert.NotAfter.ToString('u'))"
    } finally {
        $cert.Dispose()
    }
} finally {
    $ecdsa.Dispose()
}
