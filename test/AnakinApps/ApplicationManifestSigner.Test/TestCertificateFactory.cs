using AnakinRaW.AppUpdaterFramework.Security.Testing;

namespace AnakinRaW.ApplicationManifestSigner.Test;

internal static class TestCertificateFactory
{
    public static SigningKey CreateSigningKey()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        return new SigningKey(key, cert);
    }
}
