using System.Security.Cryptography.X509Certificates;
using AnakinRaW.AppUpdaterFramework.Security.Testing;

namespace AnakinRaW.ApplicationManifestCreator.Test;

/// <summary>
/// Producer-side wrapper around <see cref="TestCertificates"/> that returns a <see cref="SigningKey"/>
/// (which lives in this assembly). Kept local because <see cref="SigningKey"/> is not visible to
/// the shared testing library.
/// </summary>
internal static class TestCertificateFactory
{
    public static SigningKey CreateSigningKey()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        return new SigningKey(key, cert);
    }

    public static X509Certificate2 PublicCertOf(SigningKey key)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(key.Certificate.RawData);
#else
        return new X509Certificate2(key.Certificate.RawData);
#endif
    }
}
