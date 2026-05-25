using AnakinRaW.AppUpdaterFramework.Json;
using Xunit;
using ComponentType = AnakinRaW.AppUpdaterFramework.Metadata.Component.ComponentType;

namespace AnakinRaW.AppUpdaterFramework.Manifest.Test;

public class CanonicalManifestSerializerTests
{
    private static ApplicationManifest CreateSample(ManifestSignature? signature = null)
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

    [Fact]
    public void Serialize_OmitsSignatureField_WhenNull()
    {
        var manifest = CreateSample(signature: null);
        var json = System.Text.Encoding.UTF8.GetString(CanonicalManifestSerializer.SerializeForDigest(manifest));

        Assert.DoesNotContain("\"signature\"", json);
    }

    [Fact]
    public void Serialize_OmitsSignatureField_EvenWhenManifestIsSigned()
    {
        var manifest = CreateSample(signature: new ManifestSignature("ES256", "dummy-value", "dummy-cert"));
        var json = System.Text.Encoding.UTF8.GetString(CanonicalManifestSerializer.SerializeForDigest(manifest));

        Assert.DoesNotContain("\"signature\"", json);
        Assert.DoesNotContain("dummy-value", json);
        Assert.DoesNotContain("dummy-cert", json);
    }

    [Fact]
    public void Serialize_IsByteStableAcrossCalls()
    {
        var manifest = CreateSample();
        var a = CanonicalManifestSerializer.SerializeForDigest(manifest);
        var b = CanonicalManifestSerializer.SerializeForDigest(manifest);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Serialize_BytesIdenticalForSignedAndUnsignedSameContent()
    {
        var unsigned = CreateSample(signature: null);
        var signed = CreateSample(signature: new ManifestSignature("ES256", "v", "c"));

        var unsignedBytes = CanonicalManifestSerializer.SerializeForDigest(unsigned);
        var signedBytes = CanonicalManifestSerializer.SerializeForDigest(signed);
        Assert.Equal(unsignedBytes, signedBytes);
    }

    // The canonical form must not contain any line breaks: Utf8JsonWriter's indented output
    // emits Environment.NewLine on .NET 9+ but hardcoded "\n" on .NET Framework / older runtimes,
    // so signer (net10) and verifier (net481) would otherwise produce different bytes for the
    // same model and the signature would never verify in production.
    [Fact]
    public void Serialize_OutputContainsNoLineBreaks()
    {
        var manifest = CreateSample();
        var bytes = CanonicalManifestSerializer.SerializeForDigest(manifest);
        Assert.DoesNotContain((byte)'\n', bytes);
        Assert.DoesNotContain((byte)'\r', bytes);
    }
}
