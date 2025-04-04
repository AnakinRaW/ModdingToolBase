﻿using System;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class ApplicationUpdateConfigurationProvider : UpdateConfigurationProviderBase
{
    private readonly IApplicationEnvironment _launcherEnv;
    private readonly IFileSystem _fileSystem;

    public ApplicationUpdateConfigurationProvider(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _launcherEnv = serviceProvider.GetRequiredService<IApplicationEnvironment>();
    }

    protected override IUpdateConfiguration CreateConfiguration()
    {
        var downloadLocation = _fileSystem.Path.Combine(_launcherEnv.ApplicationLocalPath, "downloads");
        var backupsLocation = _fileSystem.Path.Combine(_launcherEnv.ApplicationLocalPath, "backups");
        return new UpdateConfiguration
        {
            DownloadRetryCount = 3,
            DownloadLocation = downloadLocation,
            BackupLocation = backupsLocation,
            BackupPolicy = BackupPolicy.Required,
            SupportsRestart = true,
            DownloadConfiguration = _launcherEnv.UpdateDownloadManagerConfiguration
        };
    }
}