using System;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public struct ExtractorAdditionalInformation
{
    public Uri? Origin { get; init; }

    public string? OverrideFileName { get; init; }

    public InstallDrive Drive { get; init; }

    public enum InstallDrive
    {
        App,
        System
    }
}