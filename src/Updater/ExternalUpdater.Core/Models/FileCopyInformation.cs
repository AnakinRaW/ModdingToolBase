using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public record FileCopyInformation
{
    [JsonPropertyName("file")]
    public required string File { get; init; }

    [JsonPropertyName("destination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Destination { get; init; }

    // SHA-256 of the source bytes. Null only for delete entries
    [JsonPropertyName("sha256")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sha256 { get; init; }
}