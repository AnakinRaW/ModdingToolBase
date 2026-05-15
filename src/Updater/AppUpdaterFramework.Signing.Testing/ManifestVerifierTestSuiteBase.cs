using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

public abstract class ManifestVerifierTestSuiteBase : TestBaseForSigning
{
    protected IManifestVerifier Verifier { get; }

    protected ManifestVerifierTestSuiteBase()
    {
        Verifier = CreateVerifier(ServiceProvider);
    }

    /// <summary>Creates the verifier instance under test.</summary>
    protected abstract IManifestVerifier CreateVerifier(IServiceProvider serviceProvider);

    /// <summary>Produces a stream containing a syntactically valid manifest with no signature block.</summary>
    protected abstract Stream CreateUnsignedManifestStream();

    /// <summary>
    /// Produces a stream containing a manifest signed with <paramref name="signingKey"/>, embedded
    /// with <paramref name="signingCert"/> and the given <paramref name="algorithm"/>.
    /// </summary>
    protected abstract Stream CreateSignedManifestStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        SignatureAlgorithm algorithm);

    /// <summary>
    /// Produces a stream containing a manifest signed with the given key, then with the signed
    /// content modified so the recomputed digest no longer matches.
    /// </summary>
    protected abstract Stream CreateTamperedContentStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        SignatureAlgorithm algorithm);

    /// <summary>
    /// Produces a stream containing a manifest with valid content and a valid certificate, but
    /// with the embedded signature value bytes flipped so signature verification fails.
    /// </summary>
    protected abstract Stream CreateCorruptedSignatureBytesStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        SignatureAlgorithm algorithm);

    /// <summary>
    /// Produces a stream containing a manifest signed by <paramref name="signingKey"/>, then with
    /// the embedded certificate replaced by <paramref name="substituteCert"/>. The signature still
    /// matches the original signing key, but the manifest now claims to be signed by the substitute.
    /// </summary>
    protected abstract Stream CreateCertSubstitutedStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        X509Certificate2 substituteCert,
        SignatureAlgorithm algorithm);

    [Fact]
    public void Verify_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Verifier.Verify(null!));
    }

    [Fact]
    public void Verify_UnsignedManifest_ReturnsMissingSignature()
    {
        using var stream = CreateUnsignedManifestStream();
        Assert.Equal(VerificationResult.MissingSignature, Verifier.Verify(stream));
    }

    [Theory]
    [InlineData(SignatureAlgorithm.ES256)]
    [InlineData(SignatureAlgorithm.ES384)]
    [InlineData(SignatureAlgorithm.ES512)]
    public void Verify_RoundTrip_Succeeds(SignatureAlgorithm algorithm)
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair(algorithm);
        using (key)
        using (cert)
        using (var stream = CreateSignedManifestStream(key, cert, algorithm))
        {
            TrustStore.Add(cert);
            Assert.Equal(VerificationResult.Ok, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_UntrustedCert_ReturnsUntrustedCert()
    {
        var (signKey, signCert) = TestCertificates.CreateEcdsaSigningPair();
        using (signKey)
        using (signCert)
        using (var stream = CreateSignedManifestStream(signKey, signCert, SignatureAlgorithm.ES256))
        {
            // Trust store intentionally empty.
            Assert.Equal(VerificationResult.UntrustedCert, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_TamperedContent_ReturnsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        using (var stream = CreateTamperedContentStream(key, cert, SignatureAlgorithm.ES256))
        {
            TrustStore.Add(cert);
            Assert.Equal(VerificationResult.SignatureInvalid, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_CorruptedSignatureBytes_ReturnsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        using (var stream = CreateCorruptedSignatureBytesStream(key, cert, SignatureAlgorithm.ES256))
        {
            TrustStore.Add(cert);
            Assert.Equal(VerificationResult.SignatureInvalid, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_SubstitutedTrustedCert_ReturnsSignatureInvalid()
    {
        // Both the original signing cert AND the substitute are in the trust store. The verifier
        // should still reject because the signature was made with the original key but the manifest
        // now embeds the substitute cert — VerifyHash against the substitute's public key fails.
        var (signKey, signCert) = TestCertificates.CreateEcdsaSigningPair();
        var (subKey, subCert) = TestCertificates.CreateEcdsaSigningPair();
        using (signKey)
        using (signCert)
        using (subKey)
        using (subCert)
        using (var stream = CreateCertSubstitutedStream(signKey, signCert, subCert, SignatureAlgorithm.ES256))
        {
            TrustStore.Add(signCert);
            TrustStore.Add(subCert);
            Assert.Equal(VerificationResult.SignatureInvalid, Verifier.Verify(stream));
        }
    }
}
