﻿using System;
using AnakinRaW.CommonUtilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;

public sealed record SingleFileDetectCondition : IDetectionCondition
{
    public ConditionType Type => ConditionType.File;

    public string Id => "SingleFile";

    public string FilePath { get; }
    
    public ComponentIntegrityInformation IntegrityInformation { get; init; }

    public Version? Version { get; init; }
    
    public SemVersion? ProductVersion { get; init; }
    
    public SingleFileDetectCondition(string filePath)
    {
        ThrowHelper.ThrowIfNullOrEmpty(filePath);
        FilePath = filePath;
    }
}