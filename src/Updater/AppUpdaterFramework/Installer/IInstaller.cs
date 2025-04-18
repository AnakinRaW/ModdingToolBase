﻿using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal interface IInstaller
{
    event EventHandler<ComponentProgressEventArgs> Progress;

    InstallResult Install(IInstallableComponent component, IFileInfo? source, IReadOnlyDictionary<string, string> variables, CancellationToken token = default);

    InstallResult Remove(IInstallableComponent component, IReadOnlyDictionary<string, string> variables, CancellationToken token = default);
}