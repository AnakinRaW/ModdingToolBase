using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Conditions;

public sealed record FileCondition : ICondition
{
    public ConditionType Type => ConditionType.File;

    public string Id => "FileCondition";

    public string FilePath { get; }

    public ConditionJoin Join { get; init; }

    public ComponentIntegrityInformation IntegrityInformation { get; init; }

    public Version? Version { get; init; }
    
    public SemVersion? ProductVersion { get; init; }
    
    public FileCondition(string filePath)
    {
        ThrowHelper.ThrowIfNullOrEmpty(filePath);
        FilePath = filePath;
    }
}