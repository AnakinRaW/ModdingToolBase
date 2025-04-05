using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Conditions;

internal sealed class FileConditionEvaluator : IConditionEvaluator
{
    public ConditionType Type => ConditionType.File;

    public bool Evaluate(IServiceProvider services, ICondition condition, IDictionary<string, string?>? properties = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (condition == null) 
            throw new ArgumentNullException(nameof(condition));
        if (condition is not FileCondition fileCondition)
            throw new ArgumentException("condition is not FileCondition", nameof(condition));

        var fileSystem = services.GetRequiredService<IFileSystem>();
        var variableResolver = services.GetRequiredService<IVariableResolver>();

        var filePath = fileCondition.FilePath;
        filePath = variableResolver.ResolveVariables(filePath, properties);
        if (string.IsNullOrEmpty(filePath) || !fileSystem.File.Exists(filePath))
            return false;
        if (fileCondition.IntegrityInformation.HashType != HashTypeKey.None)
        {
            var hashingService = services.GetRequiredService<IHashingService>();
            if (!EvaluateFileHash(hashingService, fileSystem.FileInfo.New(filePath),
                    fileCondition.IntegrityInformation.HashType, fileCondition.IntegrityInformation.Hash))
                return false;
        }

        var versionInfo = GetFileVersionInfo(filePath);

        if (fileCondition.ProductVersion is not null &&
            !EvaluateProductVersion(versionInfo, fileCondition.ProductVersion))
            return false;

        return fileCondition.Version == null || EvaluateFileVersion(versionInfo, fileCondition.Version);
    }

    private static bool EvaluateFileHash(IHashingService hashingService, IFileInfo file, HashTypeKey hashType, byte[]? expectedHash)
    {
        if (expectedHash is null)
            return false;
        var actualHash = hashingService.GetHash(file, hashType);
        return actualHash.SequenceEqual(expectedHash);
    }

    private FileVersionInfo? GetFileVersionInfo(string filePath)
    {
        try
        { 
            return FileVersionInfo.GetVersionInfo(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool EvaluateProductVersion(FileVersionInfo? versionInfo, SemVersion version)
    {
        if (versionInfo is null)
            return false;
        return SemVersion.TryParse(versionInfo.ProductVersion, SemVersionStyles.Any, out var actualVersion) &&
               actualVersion.Equals(version);
    }

    private static bool EvaluateFileVersion(FileVersionInfo? versionInfo, Version version)
    {
        if (versionInfo is null)
            return false;
        return Version.TryParse(versionInfo.FileVersion, out var actualVersion) && actualVersion.Equals(version);
    }
}