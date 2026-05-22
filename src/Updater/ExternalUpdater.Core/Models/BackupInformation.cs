using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public record BackupInformation
{
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    // SHA-256 of the backup source bytes. Null only when Source is also null
    [JsonPropertyName("sha256")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha256 { get; init; }
}