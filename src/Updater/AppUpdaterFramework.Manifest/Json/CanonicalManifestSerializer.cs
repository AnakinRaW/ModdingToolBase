using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// Produces the canonical UTF-8 byte form of a manifest used as input to the signature-digest
/// computation. Both the signer and the verifier route through this so signing input and
/// verification input are guaranteed to be byte-identical given the same model.
/// </summary>
/// <remarks>
/// Because the <see cref="ApplicationManifest.Signature"/> property is set
/// to null before serialization and the options omit null properties, the signature block never
/// appears in the canonical output.
/// </remarks>
public static class CanonicalManifestSerializer
{
    // Not indented: Utf8JsonWriter's indented output is not portable across runtimes (since
    // .NET 9 it emits Environment.NewLine, earlier versions and .NET Framework emit "\n"),
    // so signer and verifier on different platforms would compute different bytes for the
    // same model and verification would fail.
    private static readonly JsonSerializerOptions DigestOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes <paramref name="manifest"/> to its canonical UTF-8 byte form for digest
    /// computation. The <see cref="ApplicationManifest.Signature"/> property is cleared on a copy
    /// before serialization, so the resulting bytes never contain a signature block.
    /// </summary>
    /// <param name="manifest">The manifest to canonicalize.</param>
    /// <returns>The UTF-8 bytes that the signer hashes and the verifier re-hashes to match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manifest"/> is <see langword="null"/>.</exception>
    public static byte[] SerializeForDigest(ApplicationManifest manifest)
    {
        if (manifest is null)
            throw new ArgumentNullException(nameof(manifest));
        var unsigned = manifest with { Signature = null };
        return JsonSerializer.SerializeToUtf8Bytes(unsigned, DigestOptions);
    }
}
