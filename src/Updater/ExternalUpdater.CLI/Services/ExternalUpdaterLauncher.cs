using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ExternalUpdater.Services;

public class ExternalUpdaterLauncher(IServiceProvider serviceProvider) : IExternalUpdaterLauncher
{
    private readonly ICurrentProcessInfoProvider _currentProcessInfoProvider = serviceProvider.GetRequiredService<ICurrentProcessInfoProvider>();

    public Process Start(IFileInfo updater, ExternalUpdaterOptions options)
    {
        if (updater == null) 
            throw new ArgumentNullException(nameof(updater));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (!updater.Exists)
            throw new FileNotFoundException("Could not find updater application", updater.FullName);

        var startInfo = CreateStartInfo(updater.FullName, options);
        return Process.Start(startInfo)!;
    }


    private ProcessStartInfo CreateStartInfo(string updater, ExternalUpdaterOptions options)
    {

        var externalUpdateStartInfo = new ProcessStartInfo(updater)
        {
#if !DEBUG
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
#endif
        };

        if (_currentProcessInfoProvider.GetCurrentProcessInfo().IsElevated)
            externalUpdateStartInfo.Verb = "runas";

        externalUpdateStartInfo.Arguments = options.ToArgs();
        return externalUpdateStartInfo;
    }
}