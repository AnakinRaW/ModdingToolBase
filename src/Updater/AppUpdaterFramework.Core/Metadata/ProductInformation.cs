using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public record ProductInformation
{
    public required string ProductName { get; init; }

    public SemVersion? Version { get; init; }
}