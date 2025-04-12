using System;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal sealed class DownloadRepository : FileRepository, IDownloadRepository
{
    protected override IDirectoryInfo Root { get; }

    public DownloadRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        var updateConfig = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

        var location = updateConfig.DownloadLocation;
        if (string.IsNullOrEmpty(location))
            throw new InvalidOperationException("download directory not specified.");

        var root = FileSystem.DirectoryInfo.New(location);
        root.Create();

        Root = root;
    }
}