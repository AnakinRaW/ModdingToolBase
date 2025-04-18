﻿using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Detection;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public interface IInstallableComponent : IProductComponent
{
    long DownloadSize { get; }

    OriginInfo? OriginInfo { get; }

    IReadOnlyList<IDetectionCondition> DetectConditions { get; }

    InstallationSize InstallationSize { get; }
}