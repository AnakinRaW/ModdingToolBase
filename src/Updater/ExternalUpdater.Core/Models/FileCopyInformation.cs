using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public record FileCopyInformation
{
    [JsonPropertyName("file")]
    public required string File { get; init; }

    [JsonPropertyName("destination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Destination { get; init; }

    [JsonPropertyName("integrity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntegrityInformation? Integrity { get; init; }
}
