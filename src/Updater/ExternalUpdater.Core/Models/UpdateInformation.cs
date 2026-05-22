using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

public record UpdateInformation
{
    [JsonPropertyName("update")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileCopyInformation? Update { get; init; }

    [JsonPropertyName("backup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackupInformation? Backup { get; init; }
}