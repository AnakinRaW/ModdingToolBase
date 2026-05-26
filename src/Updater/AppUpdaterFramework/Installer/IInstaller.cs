using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal interface IInstaller
{
    event EventHandler<ComponentInstallProgressEventArgs> Progress;

    InstallResult Install(InstallableComponent component, string? source, IReadOnlyDictionary<string, string> variables, CancellationToken token = default);

    InstallResult Remove(InstallableComponent component, IReadOnlyDictionary<string, string> variables, CancellationToken token = default);
}