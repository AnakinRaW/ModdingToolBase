#if NET
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AnakinRaW.ApplicationManifestCreator;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using ComponentType = AnakinRaW.AppUpdaterFramework.Metadata.Component.ComponentType;

namespace AnakinRaW.AppUpdaterFramework.Manifest.Test;

/// <summary>
/// Runs <see cref="ManifestVerifierTestSuiteBase"/> against <see cref="JsonManifestVerifier"/>,
/// using <see cref="ManifestSigner"/> to produce signed manifests for the round-trip / tamper
/// hooks. Net 10 only because <c>ManifestSigner</c> lives in the producer-side
/// <c>ApplicationManifestCreator</c> assembly which is net10-only.
/// </summary>
public class JsonManifestVerifierSuite : ManifestVerifierTestSuiteBase
{
    protected override IManifestVerifier CreateVerifier(IServiceProvider serviceProvider)
        => new JsonManifestVerifier(serviceProvider);

    protected override Stream CreateUnsignedManifestStream()
        => ToStream(CreateSample());

    protected override Stream CreateSignedManifestStream(ECDsa signingKey, X509Certificate2 signingCert, SignatureAlgorithm algorithm)
        => ToStream(SignWith(CreateSample(), signingKey, signingCert, algorithm));

    protected override Stream CreateTamperedSignedManifestStream(ECDsa signingKey, X509Certificate2 signingCert, SignatureAlgorithm algorithm)
    {
        var signed = SignWith(CreateSample(), signingKey, signingCert, algorithm);
        // Mutate a field after signing — recomputed digest differs, signature won't verify.
        return ToStream(signed with { Version = "9.9.9" });
    }

    private ApplicationManifest SignWith(ApplicationManifest manifest, ECDsa key, X509Certificate2 cert, SignatureAlgorithm algorithm)
    {
        var sp = new ServiceCollection()
            .AddSingleton(ServiceProvider.GetRequiredService<IHashingService>())
            .AddSingleton(new SigningConfiguration { SignatureAlgorithm = algorithm })
            .BuildServiceProvider();
        var signer = new ManifestSigner(sp);
        // Wrap without disposing: base suite owns the key/cert lifetime and still uses the cert
        // after this method returns.
        var signingKey = new SigningKey(key, cert);
        return signer.Sign(manifest, signingKey);
    }

    private static ApplicationManifest CreateSample()
    {
        var component = new AppComponent(
            Id: "foo",
            Version: "1.0.0",
            Name: "Foo",
            Type: ComponentType.File,
            Items: null,
            OriginInfo: new OriginInfo("https://example.test/foo.dll", 1, "deadbeef"),
            InstallPath: "$(InstallDir)",
            FileName: "foo.dll",
            InstallSize: new InstallSize(1, 0),
            DetectConditions: null);
        return new ApplicationManifest("TestApp", "1.0.0", "stable", [component]);
    }

    private static Stream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }
}
#endif
