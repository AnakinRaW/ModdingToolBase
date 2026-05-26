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
    private readonly IRegistry _registry;
    private readonly string _registryPath;
    private IRegistryKey _registryKey;

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
        if (appEnvironment is null) 
            throw new ArgumentNullException(nameof(appEnvironment));

        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _registryPath = appEnvironment.UpdateRegistryPath;
        _registryKey = OpenOrCreateKey();
    }

    public void Reset()
    {
        // Wipe the whole key (including any stale values or sub keys from a previous version)
        // and recreate it empty so the live handle stays usable for subsequent reads/writes.
        _registryKey.DeleteKey(string.Empty, recursive: true);
        _registryKey.Dispose();
        _registryKey = OpenOrCreateKey();
    }

    private IRegistryKey OpenOrCreateKey()
    {
        var baseKey = _registry.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        return baseKey.CreateSubKey(_registryPath)
               ?? throw new InvalidOperationException("Unable to create registry. Missing rights?");
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
