using System;
using System.Security.Cryptography;
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

    // Guard against indented output: that would re-introduce Linux/Windows newline drift.
    [Fact]
    public void Serialize_OutputContainsNoLineBreaks()
    {
        var manifest = CreateSample();
        var bytes = CanonicalManifestSerializer.SerializeForDigest(manifest);
        Assert.DoesNotContain((byte)'\n', bytes);
        Assert.DoesNotContain((byte)'\r', bytes);
    }

    // Pin canonical bytes: any model/serialization shift trips this. Cross-OS drift needs a
    // CI matrix (ubuntu + windows) — this single-machine test won't catch that on its own.
    //
    // If a deliberate model/serialization change makes this test fail, recompute the hash by
    // temporarily printing it with the helper below and update the constant — but doing so
    // means any already-deployed signed manifests become unverifiable, so re-signing is required.
    private const string GoldenSha256 =
        "78c1680f2d4cc14d6aa591f149c9a4a5d3812e49bdda2d6d521e2cb8920fddfc";

    [Fact]
    public void Serialize_GoldenHash_Pinned()
    {
        var manifest = CreateSample();
        var bytes = CanonicalManifestSerializer.SerializeForDigest(manifest);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        Assert.Equal(GoldenSha256, hex);
    }
}
