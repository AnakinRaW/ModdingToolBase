using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal class InstallerFactory(IServiceProvider serviceProvider) : IInstallerFactory
{
    private readonly Dictionary<ComponentType, IInstaller> _installers = new();

    public IInstaller CreateInstaller(IInstallableComponent component)
    {
        if (component == null) 
            throw new ArgumentNullException(nameof(component));
        switch (component.Type)
        {
            case ComponentType.File:
                if (!_installers.ContainsKey(component.Type))
                    _installers[component.Type] = new FileInstaller(serviceProvider);
                return _installers[component.Type];
            default:
                throw new NotSupportedException($"Component type '{component.Type}' is not supported.");
        }
    }
}