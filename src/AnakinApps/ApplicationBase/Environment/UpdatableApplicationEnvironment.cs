using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using AnakinRaW.AppUpdaterFramework.Configuration;

namespace AnakinRaW.ApplicationBase.Environment;

public abstract class UpdatableApplicationEnvironment(Assembly assembly, IFileSystem fileSystem)
    : ApplicationEnvironment(assembly, fileSystem), IUpdateConfigurationProvider
{
    [field:MaybeNull, AllowNull]
    public UpdateConfiguration UpdateConfiguration
    {
        get
        {
            if (field is null)
            {
                field = CreateUpdateConfiguration();
                if (field.RestartConfiguration.SupportsRestart && !IsRunningOnNetFramework())
                    throw new NotSupportedException("Restarting is only supported for .NET Framework applications.");
            }

            return CreateUpdateConfiguration();
        }
    }

    public abstract Uri? RepositoryUrl { get; }

    public abstract ICollection<Uri> UpdateMirrors { get; }

    public abstract string UpdateRegistryPath { get; } 
    
    public UpdateConfiguration GetConfiguration()
    {
        return UpdateConfiguration;
    }

    protected abstract UpdateConfiguration CreateUpdateConfiguration();

    private static bool IsRunningOnNetFramework()
    {
        return RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
    }
}