using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

/// <summary>Describes a single update operation, consisting of an optional file copy and an optional backup that can be restored on failure.</summary>
public record UpdateInformation
{
    /// <summary>Gets the file copy operation to be applied as part of the update.</summary>
    /// <value>The copy operation, or <see langword="null"/> when this entry only carries a backup.</value>
    [JsonPropertyName("update")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileCopyInformation? Update { get; init; }

    /// <summary>Gets the backup that is restored when <see cref="Update"/> fails.</summary>
    /// <value>The backup descriptor, or <see langword="null"/> when no backup is available for this entry.</value>
    [JsonPropertyName("backup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackupInformation? Backup { get; init; }
}