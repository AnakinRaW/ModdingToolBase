using System;
using System.Buffers.Binary;
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
        AddCaExtensions(req);
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
    /// ES384→P-384/SHA-384, ES512→P-521/SHA-512. The cert is marked as a self-signed CA so chain
    /// validation accepts it as both signer and trust anchor. The caller owns both returned objects.
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
        AddCaExtensions(req);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(20));
        return (ecdsa, cert);
    }

    /// <summary>
    /// Generates a root + intermediate pair modelling the production trust hierarchy: a self-signed
    /// long-lived root CA whose private key is discarded, and an intermediate signed by the root
    /// that the caller uses to sign data using to ECDSA P-256 / SHA-256.
    /// </summary>
    /// <param name="rootSubject">Subject DN for the root CA.</param>
    /// <param name="intermediateSubject">Subject DN for the intermediate.</param>
    /// <param name="intermediateNotBefore">Defaults to 5 minutes ago. Override to test certs that aren't yet valid.</param>
    /// <param name="intermediateNotAfter">Defaults to 1 year from now. Override to test expired certs.</param>
    /// <returns>
    /// (root public cert, intermediate's private key, intermediate's public cert).
    /// The root's private key is intentionally not returned — production roots stay offline.
    /// Caller owns and must dispose all three items.
    /// </returns>
    public static (X509Certificate2 RootCert, ECDsa IntermediateKey, X509Certificate2 IntermediateCert) CreateRootAndIntermediate(
        string rootSubject = "CN=Test Root CA",
        string intermediateSubject = "CN=Test Intermediate Signer",
        DateTimeOffset? intermediateNotBefore = null,
        DateTimeOffset? intermediateNotAfter = null)
    {
        X509Certificate2 rootPublicOnly;
        X509Certificate2 intermediateCert;
        ECDsa intermediateKey;

        using (var rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            var rootReq = new CertificateRequest(rootSubject, rootKey, HashAlgorithmName.SHA256);
            AddCaExtensions(rootReq);
            // Wide root validity so test intermediates can be created with notBefore well
            // in the past (for "expired" test cases) or in the future ("not-yet-valid") without
            // tripping CertificateRequest.Create's "notBefore must be within issuer's window" check.
            using var rootWithKey = rootReq.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddYears(-25),
                DateTimeOffset.UtcNow.AddYears(25));

            intermediateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            try
            {
                var intReq = new CertificateRequest(intermediateSubject, intermediateKey, HashAlgorithmName.SHA256);
                intReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
                intReq.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature, true));
                intReq.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(
                    intReq.PublicKey, false));

                intermediateCert = intReq.Create(
                    rootWithKey,
                    intermediateNotBefore ?? DateTimeOffset.UtcNow.AddMinutes(-5),
                    intermediateNotAfter ?? DateTimeOffset.UtcNow.AddYears(1),
                    NewSerial());
            }
            catch
            {
                intermediateKey.Dispose();
                throw;
            }

            // Return the root with no private key — production roots stay offline; tests
            // shouldn't accidentally re-use the root's key for signing.
#if NET9_0_OR_GREATER
            rootPublicOnly = X509CertificateLoader.LoadCertificate(rootWithKey.RawData);
#else
            rootPublicOnly = new X509Certificate2(rootWithKey.RawData);
#endif
        }

        return (rootPublicOnly, intermediateKey, intermediateCert);
    }

    private static void AddCaExtensions(CertificateRequest req)
    {
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.DigitalSignature, true));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
    }

    private static byte[] NewSerial()
    {
        var serial = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(serial, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return serial;
    }
}
