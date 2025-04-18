﻿using AnakinRaW.AppUpdaterFramework.Updater.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal readonly struct InstallerInteractionResult(InstallResult installResult, bool retry = false)
{
    public bool Retry { get; } = retry;

    public InstallResult InstallResult { get; } = installResult;
}