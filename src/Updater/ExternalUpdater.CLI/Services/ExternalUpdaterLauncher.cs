﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.ExternalUpdater.Services;

public class ExternalUpdaterLauncher : IExternalUpdaterLauncher
{
    private readonly ICurrentProcessInfoProvider _currentProcessInfoProvider;

    public ExternalUpdaterLauncher(IServiceProvider serviceProvider)
    {
        _currentProcessInfoProvider = serviceProvider.GetRequiredService<ICurrentProcessInfoProvider>();
    }

    public Process Start(IFileInfo updater, ExternalUpdaterOptions options)
    {
        Requires.NotNull(updater, nameof(updater));
        Requires.NotNull(options, nameof(options));

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