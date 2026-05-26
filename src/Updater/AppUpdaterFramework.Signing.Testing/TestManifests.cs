using AnakinRaW.AppUpdaterFramework.Json;
using ComponentType = AnakinRaW.AppUpdaterFramework.Metadata.Component.ComponentType;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

/// <summary>
/// Factory for sample <see cref="ApplicationManifest"/> instances used in test setups across
/// the verifier, signer, and serializer test projects.
/// </summary>
public static class TestManifests
{
    /// <summary>
    /// Returns a small, well-formed sample manifest. Pass <paramref name="signature"/> non-null to
    /// pre-populate the signature block (useful for negative-path tests); leave null for the
    /// unsigned form.
    /// </summary>
    public static ApplicationManifest CreateSample(ManifestSignature? signature = null)
    {
        var component = new AppComponent(
            Id: "foo",
            Version: "1.2.3",
            Name: "Foo Component",
            Type: ComponentType.File,
            Items: null,
            OriginInfo: new OriginInfo("https://example.test/foo.dll", 1234, "deadbeef"),
            InstallPath: "$(InstallDir)",
            FileName: "foo.dll",
            InstallSize: new InstallSize(1024, 0),
            DetectConditions: null);
        return new ApplicationManifest("TestApp", "1.0.0", "stable", [component]) { Signature = signature };
    }
}
