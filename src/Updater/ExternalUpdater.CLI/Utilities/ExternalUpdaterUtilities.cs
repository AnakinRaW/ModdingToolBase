﻿using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Semver;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace AnakinRaW.ExternalUpdater.Utilities;

internal static class ExternalUpdaterUtilities
{
    public static bool IsValidAssembly(Stream assemblyStream, out ExternalUpdaterInformation? assemblyInformation)
    {
        assemblyInformation = null;

        var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyStream);
        var moduleDefinition = assemblyDef.MainModule;

        var binaryName = moduleDefinition.Name;

        if (!binaryName.Equals(ExternalUpdaterConstants.AppUpdaterModuleName))
            return false;

        var assemblyNameInfo = assemblyDef.Name;

        var name = assemblyNameInfo.Name;

        try
        {
            var fileVersion = GetFileVersion(assemblyDef);
            var infoVersion = GetInformationalVersion(assemblyDef);

            assemblyInformation = new ExternalUpdaterInformation
            {
                Name = name,
                FileVersion = fileVersion,
                InformationalVersion = infoVersion
            };

            return true;
        }
        catch (Exception)
        {
            assemblyInformation = null;
            return false;
        }
    }

    private static Version GetFileVersion(ICustomAttributeProvider assemblyDefinition)
    {
        var fileVersion = assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(AssemblyFileVersionAttribute));
        if (fileVersion is null)
            throw new NotSupportedException($"Could not find {nameof(AssemblyFileVersionAttribute)} in assembly");
        return Version.Parse(fileVersion);
    }

    private static SemVersion GetInformationalVersion(ICustomAttributeProvider assemblyDefinition)
    {
        var infoVersion = assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(AssemblyInformationalVersionAttribute));
        if (infoVersion is null)
            throw new NotSupportedException($"Could not find {nameof(AssemblyInformationalVersionAttribute)} in assembly");
        return SemVersion.Parse(infoVersion, SemVersionStyles.Any);
    }
}