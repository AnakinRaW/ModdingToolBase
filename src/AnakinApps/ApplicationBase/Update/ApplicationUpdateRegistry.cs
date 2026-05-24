using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.CommonUtilities.Registry;

namespace AnakinRaW.ApplicationBase.Update;

// HKCU keys for the deferred-update resume gate:
//   - RequiresUpdate (bool)   — the resume gate itself
//   - UpdateBranch   (string) — branch name; lets the framework pick the right mirror for re-download if needed
//   - ResetApp       (bool)   — request a full app reset on next launch
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
        set => _registryKey.SetValue(nameof(RequiresUpdate), value);
    }

    public string? UpdateBranch
    {
        get => _registryKey.GetValueOrDefault<string?>(nameof(UpdateBranch), null, out _);
        set
        {
            if (string.IsNullOrEmpty(value))
                _registryKey.DeleteValue(nameof(UpdateBranch));
            else
                _registryKey.SetValue(nameof(UpdateBranch), value!);
        }
    }

    public ApplicationUpdateRegistry(IRegistry registry, UpdatableApplicationEnvironment appEnvironment)
    {
        if (registry is null) throw new ArgumentNullException(nameof(registry));
        if (appEnvironment is null) throw new ArgumentNullException(nameof(appEnvironment));

        var baseKey = registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        _registryKey = baseKey.CreateSubKey(appEnvironment.UpdateRegistryPath)
                        ?? throw new InvalidOperationException("Unable to create registry. Missing rights?");
    }

    public void Reset()
    {
        _registryKey.DeleteValue(nameof(ResetApp));
        _registryKey.DeleteValue(nameof(RequiresUpdate));
        _registryKey.DeleteValue(nameof(UpdateBranch));
    }

    public void ScheduleReset()
    {
        ResetApp = true;
    }

    public void Dispose()
    {
        _registryKey.Dispose();
    }
}
