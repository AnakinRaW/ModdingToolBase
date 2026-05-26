using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

/// <summary>Describes the expected hash of a file used by the external updater to verify integrity before applying or restoring it.</summary>
public readonly record struct IntegrityInformation
{
    /// <summary>Gets the name of the hash algorithm used to compute <see cref="Hash"/>, such as <c>SHA256</c> or <c>MD5</c>.</summary>
    /// <value>The case-insensitive name of a supported hash algorithm.</value>
    [JsonPropertyName("hashType")]
    public required string HashType { get; init; }

    /// <summary>Gets the expected hash value of the file, encoded as a hexadecimal string.</summary>
    /// <value>The hexadecimal representation of the expected hash bytes.</value>
    [JsonPropertyName("hash")]
    public required string Hash { get; init; }
}
