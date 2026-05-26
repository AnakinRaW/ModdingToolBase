using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Security;

public class SignatureVerifierTests : TestBaseForSigning
{
    private readonly ISignatureVerifier _verifier;

    public SignatureVerifierTests()
    {
        _verifier = new SignatureVerifier(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IFileSystem>(new RealFileSystem());
    }

    [Theory]
    [InlineData(SignatureAlgorithm.ES256)]
    [InlineData(SignatureAlgorithm.ES384)]
    [InlineData(SignatureAlgorithm.ES512)]
    public void Verify_RoundTripTrustedCert_ReturnsOk(SignatureAlgorithm algorithm)
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair(algorithm);
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var parsed = Sign(key, cert, "hello, world", algorithm);
            Assert.Equal(VerificationResult.Ok, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_UntrustedCert_ReturnsUntrustedCert()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            // Trust store intentionally empty.
            var parsed = Sign(key, cert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_UnsupportedAlgorithm_ReturnsUnsupportedAlgorithm()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var parsed = Sign(key, cert, "x", SignatureAlgorithm.ES256) with { Algorithm = "PS256" };
            Assert.Equal(VerificationResult.UnsupportedAlgorithm, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_TamperedContent_ReturnsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var parsed = Sign(key, cert, "hello, world", SignatureAlgorithm.ES256);
            var tampered = parsed with { CanonicalBytes = Encoding.UTF8.GetBytes("hello, mars!") };
            Assert.Equal(VerificationResult.SignatureInvalid, _verifier.Verify(tampered));
        }
    }

    [Fact]
    public void Verify_CorruptedSignatureBytes_ReturnsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var parsed = Sign(key, cert, "hello, world", SignatureAlgorithm.ES256);
            parsed.SignatureValue[parsed.SignatureValue.Length - 1] ^= 0xFF;
            Assert.Equal(VerificationResult.SignatureInvalid, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_GarbageCertDer_ReturnsMalformedSignatureBlock()
    {
        var parsed = new ParsedSignature("ES256", [], [0x01, 0x02, 0x03], Encoding.UTF8.GetBytes("x"));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(parsed));
    }

    [Fact]
    public void Verify_CertWithoutEcdsaKey_ReturnsMalformedSignatureBlock()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=RSA Cert", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        TrustStore.Add(cert);

        var parsed = new ParsedSignature("ES256", new byte[64], cert.RawData, Encoding.UTF8.GetBytes("x"));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(parsed));
    }

    [Fact]
    public void Verify_NullParsed_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _verifier.Verify(null!));
    }

    [Fact]
    public void Verify_IntermediateChainsToTrustedRoot_ReturnsOk()
    {
        var (root, intKey, intCert) = TestCertificates.CreateRootAndIntermediate();
        using (root)
        using (intKey)
        using (intCert)
        {
            // Trust ONLY the root — the central design property.
            TrustStore.Add(root);
            var parsed = Sign(intKey, intCert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.Ok, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_IntermediateWithoutTrustedRoot_ReturnsUntrustedCert()
    {
        var (root, intKey, intCert) = TestCertificates.CreateRootAndIntermediate();
        using (root)
        using (intKey)
        using (intCert)
        {
            // Trust store intentionally does NOT contain `root`.
            var parsed = Sign(intKey, intCert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_IntermediateChainsToDifferentRoot_ReturnsUntrustedCert()
    {
        var (rootA, intKey, intCert) = TestCertificates.CreateRootAndIntermediate(rootSubject: "CN=Test Root A");
        using var rootB = TestCertificates.CreateSelfSigned("CN=Test Root B");
        using (rootA)
        using (intKey)
        using (intCert)
        {
            // We trust a DIFFERENT root than the one that signed the intermediate.
            TrustStore.Add(rootB);
            var parsed = Sign(intKey, intCert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_ExpiredIntermediate_ReturnsUntrustedCert()
    {
        var (root, intKey, intCert) = TestCertificates.CreateRootAndIntermediate(
            intermediateNotBefore: DateTimeOffset.UtcNow.AddYears(-2),
            intermediateNotAfter: DateTimeOffset.UtcNow.AddYears(-1));
        using (root)
        using (intKey)
        using (intCert)
        {
            TrustStore.Add(root);
            var parsed = Sign(intKey, intCert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(parsed));
        }
    }

    [Fact]
    public void Verify_NotYetValidIntermediate_ReturnsUntrustedCert()
    {
        var (root, intKey, intCert) = TestCertificates.CreateRootAndIntermediate(
            intermediateNotBefore: DateTimeOffset.UtcNow.AddYears(1),
            intermediateNotAfter: DateTimeOffset.UtcNow.AddYears(2));
        using (root)
        using (intKey)
        using (intCert)
        {
            TrustStore.Add(root);
            var parsed = Sign(intKey, intCert, "hello, world", SignatureAlgorithm.ES256);
            Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(parsed));
        }
    }

    private static ParsedSignature Sign(ECDsa key, X509Certificate2 cert, string content, SignatureAlgorithm algorithm)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        using HashAlgorithm hasher = algorithm switch
        {
            SignatureAlgorithm.ES256 => SHA256.Create(),
            SignatureAlgorithm.ES384 => SHA384.Create(),
            SignatureAlgorithm.ES512 => SHA512.Create(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
        var digest = hasher.ComputeHash(contentBytes);
        var signature = key.SignHash(digest);
        return new ParsedSignature(algorithm.ToString(), signature, cert.RawData, contentBytes);
    }
}
