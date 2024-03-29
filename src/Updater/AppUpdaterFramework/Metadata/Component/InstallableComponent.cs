using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Conditions;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class InstallableComponent(IProductComponentIdentity identity, OriginInfo? originInfo)
    : ProductComponent(identity), IInstallableComponent
{
    public long DownloadSize => OriginInfo?.Size ?? 0;
    public OriginInfo? OriginInfo { get; } = originInfo;
    public IReadOnlyList<ICondition> DetectConditions { get; init; } = Array.Empty<ICondition>();
    public InstallationSize InstallationSize { get; init; }

    public abstract string? GetFullPath(IServiceProvider serviceProvider, ProductVariables? variables = null);
}