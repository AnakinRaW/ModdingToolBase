using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal sealed class DownloadRepositoryFactory(IServiceProvider serviceProvider) : IDownloadRepositoryFactory
{
    [field:AllowNull, MaybeNull]
    private IFileRepository FileRepository => LazyInitializer.EnsureInitialized(ref field, CreateRepository)!;

    private IFileRepository CreateRepository()
    {
        var config = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        return new FileRepository(config.DownloadLocation, fileSystem);
    }

    public IReadOnlyFileRepository GetReadOnlyRepository()
    {
        return GetRepository();
    }

    public IFileRepository GetRepository()
    {
        return FileRepository;
    }
}