﻿using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ApplicationBase.Update;

public sealed class ApplicationUpdateRegistry : IDisposable
{
    private readonly IRegistryKey _registryKey;

    public bool ResetApp
    {
        get => _registryKey.GetValueOrDefault(nameof(ResetApp), false, out _);
        set => _registryKey.SetValue(nameof(ResetApp), value);
    }

    public bool RequiresUpdate
    {
        get => _registryKey.GetValueOrDefault(nameof(RequiresUpdate), false, out _);
        private set => _registryKey.SetValue(nameof(RequiresUpdate), value);
    }

    public string? UpdateCommandArgs
    {
        get => _registryKey.GetValueOrDefault<string?>(nameof(UpdateCommandArgs), null, out _);
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
        get => _registryKey.GetValueOrDefault<string?>(nameof(UpdaterPath), null, out _);
        private set
        {
            if (string.IsNullOrEmpty(value))
                _registryKey.DeleteValue(nameof(UpdaterPath));
            else
                _registryKey.SetValue(nameof(UpdaterPath), value!);
        }
    }

    public string? UpdateBranch
    {
        get => _registryKey.GetValueOrDefault<string?>(nameof(UpdateBranch), null, out _);
        private set
        {
            if (string.IsNullOrEmpty(value))
                _registryKey.DeleteValue(nameof(UpdateBranch));
            else
                _registryKey.SetValue(nameof(UpdateBranch), value!);
        }
    }

    public ApplicationUpdateRegistry(IRegistry registry, UpdatableApplicationEnvironment appEnvironment)
    {
        if (registry == null) 
            throw new ArgumentNullException(nameof(registry));
        if (appEnvironment is null) 
            throw new ArgumentNullException(nameof(appEnvironment));
        var baseKey = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        var registryKey = baseKey.CreateSubKey(appEnvironment.UpdateRegistryPath);
        _registryKey = registryKey ?? throw new InvalidOperationException("Unable to create registry. Missing rights?");
    }
    
    public void Reset()
    {
        _registryKey.DeleteValue(nameof(ResetApp));
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

        // We don't want to store the current arguments in the registry, as they are only relevant for immediate restart.
        // Adding them for delayed updates might cause usability and security issues.
        var optionsWithoutCurrentArgs = options with { AppToStartArguments = null };

        UpdateCommandArgs = optionsWithoutCurrentArgs.ToArgs();
    }

    public void ScheduleReset()
    {
        ResetApp = true;
    }

    public void Dispose()
    {
        _registryKey.Dispose();
    }

    public void SetBranch(string? branchName)
    {
        UpdateBranch = branchName;
    }
}