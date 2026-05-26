using System.Text.Json.Serialization;

namespace AnakinRaW.ExternalUpdater.Models;

/// <summary>Describes a single file copy or delete operation performed by the external updater.</summary>
public record FileCopyInformation
{
    /// <summary>Gets the absolute path of the source file to copy.</summary>
    /// <value>The absolute path of the source file.</value>
    [JsonPropertyName("file")]
    public required string File { get; init; }

    /// <summary>Gets the absolute path the source file is moved to.</summary>
    /// <value>The absolute destination path, or <see langword="null"/> when the source file should be deleted instead of copied.</value>
    [JsonPropertyName("destination")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Destination { get; init; }

    /// <summary>Gets the expected integrity information of <see cref="File"/> that must match before the copy is performed.</summary>
    /// <value>The integrity descriptor of the source file, or <see langword="null"/> when no integrity check is performed.</value>
    [JsonPropertyName("integrity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntegrityInformation? Integrity { get; init; }
}
