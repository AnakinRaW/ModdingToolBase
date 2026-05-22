using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public readonly record struct IntegrityInformation
{
    [JsonPropertyName("hashType")]
    public required string HashType { get; init; }

    [JsonPropertyName("hash")]
    public required string Hash { get; init; }
}
