using System;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public record ComponentFileInformation
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public required Version FileVersion { get; init; }

    public required long Size { get; init; }

    public required byte[] Hash { get; init; }

    public string? Name { get; init; }

    public SemVersion? InformationalVersion { get; init; }
}