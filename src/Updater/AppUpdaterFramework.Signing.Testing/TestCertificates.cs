using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

/// <summary>
/// Helpers for fabricating self-signed certificates and ECDSA signing key pairs for use in tests.
/// </summary>
public static class TestCertificates
{
    /// <summary>
    /// Creates a fresh ECDSA P-256 self-signed certificate. The private key is not attached to the
    /// returned instance; callers receive the public certificate only.
    /// </summary>
    public static X509Certificate2 CreateSelfSigned(string subject = "CN=Test Trust Anchor")
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var req = new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256);
        using var withKey = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(20));
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(withKey.RawData);
#else
        return new X509Certificate2(withKey.RawData);
#endif
    }

    /// <summary>
    /// Creates a fresh signing key pair (ECDSA private key + matching self-signed cert) for the
    /// given <paramref name="algorithm"/>. Curve and hash are paired per FIPS 186-4: ES256→P-256/SHA-256,
    /// ES384→P-384/SHA-384, ES512→P-521/SHA-512. The caller owns both returned objects.
    /// </summary>
    public static (ECDsa Key, X509Certificate2 Certificate) CreateEcdsaSigningPair(
        SignatureAlgorithm algorithm = SignatureAlgorithm.ES256,
        string subject = "CN=Test Signer")
    {
        var (curve, hash) = algorithm switch
        {
            SignatureAlgorithm.ES256 => (ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256),
            SignatureAlgorithm.ES384 => (ECCurve.NamedCurves.nistP384, HashAlgorithmName.SHA384),
            SignatureAlgorithm.ES512 => (ECCurve.NamedCurves.nistP521, HashAlgorithmName.SHA512),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
        var ecdsa = ECDsa.Create(curve);
        var req = new CertificateRequest(subject, ecdsa, hash);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(20));
        return (ecdsa, cert);
    }
}
