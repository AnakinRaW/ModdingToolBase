using System;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Services;

internal sealed class ApplicationUpdaterRegistry : IApplicationUpdaterRegistry
{
    private readonly IRegistryKey _registryKey;

    public bool Reset
    {
        get => _registryKey.GetValueOrDefault(nameof(Reset), false, out _);
        private set => _registryKey.SetValue(nameof(Reset), value);
    }

    public bool RequiresUpdate
    {
        get => _registryKey.GetValueOrDefault(nameof(RequiresUpdate), false, out _);
        private set => _registryKey.SetValue(nameof(RequiresUpdate), value);
    }

    public string? UpdateCommandArgs
    {
        get => _registryKey.GetValueOrDefault(nameof(UpdateCommandArgs), (string?)null, out bool _);
        private set
        {
            if (string.IsNullOrEmpty(value))
                _registryKey.DeleteValue(nameof(UpdateCommandArgs));
            else
                _registryKey.SetValue(nameof(UpdateCommandArgs), value!);
        }
    }

    public string? UpdaterPath
    {
        get => _registryKey.GetValueOrDefault(nameof(UpdaterPath), (string)null!, out _);
        private set
        {
            if (string.IsNullOrEmpty(value))
                _registryKey.DeleteValue(nameof(UpdaterPath));
            else
                _registryKey.SetValue(nameof(UpdaterPath), value!);
        }
    }

    public ApplicationUpdaterRegistry(string basePath, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        var registry = serviceProvider.GetRequiredService<IRegistry>();
        var baseKey = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        var registryKey = baseKey.CreateSubKey(basePath);
        _registryKey = registryKey ?? throw new InvalidOperationException("Unable to create registry. Missing rights?");
    }

    public void ScheduleReset()
    {
        Reset = true;
    }

    public void Clear()
    {
        _registryKey.DeleteValue(nameof(Reset));
        _registryKey.DeleteValue(nameof(RequiresUpdate));
        _registryKey.DeleteValue(nameof(UpdateCommandArgs));
        _registryKey.DeleteValue(nameof(UpdaterPath));
    }

    public void ScheduleUpdate(IFileInfo updater, ExternalUpdaterOptions options)
    {
        if (updater == null)
            throw new ArgumentNullException(nameof(updater));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        RequiresUpdate = true;
        UpdaterPath = updater.FullName;
        UpdateCommandArgs = options.ToArgs();
    }
}