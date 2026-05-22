using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public record BackupInformation
{
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }
}