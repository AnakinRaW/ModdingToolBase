using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Extraction;

internal sealed record ExtractedProductInformation
{
    public required string ProductName { get; init; }

    public SemVersion? Version { get; init; }
}