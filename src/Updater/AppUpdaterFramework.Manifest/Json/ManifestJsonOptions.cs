using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// Single source of <see cref="JsonSerializerOptions"/> for writing application manifests.
/// Both the manifest creator and the manifest signer route through this so on-disk output and
/// signing input use exactly the same byte representation.
/// </summary>
public static class ManifestJsonOptions
{
    /// <summary>
    /// The canonical write settings. Indented, enums as strings, null properties omitted.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
