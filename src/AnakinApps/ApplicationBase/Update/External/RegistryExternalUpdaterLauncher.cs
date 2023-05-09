using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update.External;

internal sealed class RegistryExternalUpdaterLauncher : IRegistryExternalUpdaterLauncher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IApplicationUpdaterRegistry _registry;
    private readonly IExternalUpdaterLauncher _launcher;
    private readonly IFileSystem _fileSystem;

    public RegistryExternalUpdaterLauncher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _registry = serviceProvider.GetRequiredService<IApplicationUpdaterRegistry>();
        _launcher = serviceProvider.GetRequiredService<IExternalUpdaterLauncher>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    }

    public void Launch()
    {
        var updaterPath = _registry.UpdaterPath;
        if (string.IsNullOrEmpty(updaterPath))
            throw new NotSupportedException("No updater in registry set");
        var updater = _fileSystem.FileInfo.New(updaterPath!);

        var updateArgs = _registry.UpdateCommandArgs;
        if (updateArgs is null)
            throw new NotSupportedException("No updater options set.");

        // TODO: CPI to CommonUtils
        var cpi = CurrentProcessInfo.Current;

        var loggingPath = _serviceProvider.GetRequiredService<IPathHelperService>()
            .NormalizePath(_fileSystem.Path.GetTempPath(), PathNormalizeOptions.Full);

        var launchOptions = ExternalUpdaterArgumentUtilities.FromArgs(updateArgs)
            .WithCurrentData(cpi.ProcessFilePath, cpi.Id, loggingPath, _serviceProvider);
        _launcher.Start(updater, launchOptions);
    }
}