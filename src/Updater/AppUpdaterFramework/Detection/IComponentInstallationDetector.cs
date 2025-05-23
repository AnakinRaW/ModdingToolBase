﻿using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Detection;

public interface IComponentInstallationDetector
{
    bool IsInstalled(InstallableComponent component, IReadOnlyDictionary<string, string> variables);
}