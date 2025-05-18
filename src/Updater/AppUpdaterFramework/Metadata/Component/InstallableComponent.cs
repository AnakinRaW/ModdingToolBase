using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class InstallableComponent(string id, SemVersion? version, OriginInfo? originInfo)
    : ProductComponent(id, version)
{
    public long DownloadSize => OriginInfo?.Size ?? 0;

    public OriginInfo? OriginInfo { get; } = originInfo;

    public IReadOnlyList<IDetectionCondition> DetectConditions { get; init; } = [];

    public InstallationSize InstallationSize { get; init; }
}