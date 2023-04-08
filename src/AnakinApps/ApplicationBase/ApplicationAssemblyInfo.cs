﻿using Semver;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace AnakinRaW.ApplicationBase;

public class ApplicationAssemblyInfo
{
    public Assembly CurrentAssembly { get; }

    public string? ProductName { get; }

    public string? InformationalVersion { get; }

    public string? FileVersion { get; }

    public string? AssemblyVersion { get; }

    public string? Title { get; }

    public string ExecutableFileName { get; }

    public ApplicationAssemblyInfo(Assembly assembly)
    {
        CurrentAssembly = assembly;
        InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        FileVersion = fileVersionInfo.FileVersion;
        ProductName = fileVersionInfo.ProductName;
        AssemblyVersion = assembly.GetName().Version?.ToString();
        Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        ExecutableFileName = assembly.Modules.First().Name;
    }

    public SemVersion? InformationalAsSemVer()
    {
        if (InformationalVersion is null)
            return null;
        SemVersion.TryParse(InformationalVersion, SemVersionStyles.Any, out var version);
        return version;
    }
}