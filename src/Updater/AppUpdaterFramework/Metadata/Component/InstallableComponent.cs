using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Detection;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class InstallableComponent(IProductComponentIdentity identity, OriginInfo? originInfo)
    : ProductComponent(identity), IInstallableComponent
{
    public long DownloadSize => OriginInfo?.Size ?? 0;
    public OriginInfo? OriginInfo { get; } = originInfo;
    public IReadOnlyList<IDetectionCondition> DetectConditions { get; init; } = Array.Empty<IDetectionCondition>();
    public InstallationSize InstallationSize { get; init; }

    public abstract string? GetFullPath(IServiceProvider serviceProvider, IReadOnlyDictionary<string, string> variables);
}