using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update.External;

internal sealed class RegistryExternalUpdaterLauncher(IServiceProvider serviceProvider)
    : IRegistryExternalUpdaterLauncher
{
    private readonly IApplicationUpdaterRegistry _registry = serviceProvider.GetRequiredService<IApplicationUpdaterRegistry>();
    private readonly IExternalUpdaterLauncher _launcher = serviceProvider.GetRequiredService<IExternalUpdaterLauncher>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ICurrentProcessInfoProvider  _currentProcessInfoProvider = serviceProvider.GetRequiredService<ICurrentProcessInfoProvider>();

    public void Launch()
    {
        var updaterPath = _registry.UpdaterPath;
        if (string.IsNullOrEmpty(updaterPath))
            throw new NotSupportedException("No updater in registry set");
        var updater = _fileSystem.FileInfo.New(updaterPath!);

        var updateArgs = _registry.UpdateCommandArgs;
        if (updateArgs is null)
            throw new NotSupportedException("No updater options set.");

        var cpi = _currentProcessInfoProvider.GetCurrentProcessInfo();
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file");

        // Must be trimmed as otherwise paths enclosed in quotes and a trailing separator
        // cause commandline arg parsing errors
        var loggingPath = PathNormalizer.Normalize(_fileSystem.Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators);

        var launchOptions = ExternalUpdaterArgumentUtilities.FromArgs(updateArgs)
            .WithCurrentData(cpi.ProcessFilePath, cpi.Id, loggingPath, serviceProvider);
        _launcher.Start(updater, launchOptions);
    }
}