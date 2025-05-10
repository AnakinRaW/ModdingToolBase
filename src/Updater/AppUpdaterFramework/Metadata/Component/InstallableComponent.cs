using AnakinRaW.AppUpdaterFramework.Detection;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class InstallableComponent(IProductComponentIdentity identity, OriginInfo? originInfo)
    : ProductComponent(identity), IInstallableComponent
{
    public long DownloadSize => OriginInfo?.Size ?? 0;

    public OriginInfo? OriginInfo { get; } = originInfo;

    public IReadOnlyList<IDetectionCondition> DetectConditions { get; init; } = [];

    public InstallationSize InstallationSize { get; init; }

    public abstract string? GetFullPath(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables);
}