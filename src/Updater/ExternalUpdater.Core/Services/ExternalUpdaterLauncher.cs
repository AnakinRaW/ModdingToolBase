using AnakinRaW.CommonUtilities;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace AnakinRaW.ExternalUpdater.Services;

public sealed class ExternalUpdaterLauncher(IServiceProvider serviceProvider) : IExternalUpdaterLauncher
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ExternalUpdaterLauncher));

    public Process Start(IFileInfo updater, ExternalUpdaterOptions options)
    {
        if (updater == null) 
            throw new ArgumentNullException(nameof(updater));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (!updater.Exists)
            throw new FileNotFoundException("Could not find updater application", updater.FullName);

        var startInfo = CreateStartInfo(updater.FullName, options);
        _logger?.LogTrace("Starting external update with process info: {FileName} {Args}", startInfo.FileName, startInfo.Arguments);
        return Process.Start(startInfo)!;
    }


    private ProcessStartInfo CreateStartInfo(string updater, ExternalUpdaterOptions options)
    {
        var externalUpdateStartInfo = new ProcessStartInfo(updater)
        {
            UseShellExecute = true,
#if !DEBUG
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
#endif
        };

        if (CurrentProcessInfo.Current.IsElevated)
            externalUpdateStartInfo.Verb = "runas";

        externalUpdateStartInfo.Arguments = options.ToArgs();
        return externalUpdateStartInfo;
    }
}