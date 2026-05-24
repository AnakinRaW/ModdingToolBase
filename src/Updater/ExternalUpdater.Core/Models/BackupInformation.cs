using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

/// <summary>Describes a file that may be restored by the external updater when an update fails.</summary>
public record BackupInformation
{
    /// <summary>Gets the absolute path of the file the backup belongs to and that is restored when the backup is applied.</summary>
    /// <value>The absolute destination path of the backed-up file.</value>
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    /// <summary>Gets the absolute path of the backup source file used to restore <see cref="Destination"/>.</summary>
    /// <value>The absolute path of the backup file, or <see langword="null"/> when the destination should simply be deleted on restore.</value>
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    /// <summary>Gets the expected integrity information of <see cref="Source"/> that must match before the backup is restored.</summary>
    /// <value>The integrity descriptor of the backup source, or <see langword="null"/> when no integrity check is performed.</value>
    [JsonPropertyName("integrity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntegrityInformation? Integrity { get; init; }
}
