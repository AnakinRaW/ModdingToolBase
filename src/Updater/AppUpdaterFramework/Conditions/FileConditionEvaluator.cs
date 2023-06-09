﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Conditions;

internal sealed class FileConditionEvaluator : IConditionEvaluator
{
    public ConditionType Type => ConditionType.File;

    public bool Evaluate(IServiceProvider services, ICondition condition, IDictionary<string, string?>? properties = null)
    {
        Requires.NotNull(services, nameof(services));
        Requires.NotNull(condition, nameof(condition));
        if (condition is not FileCondition fileCondition)
            throw new ArgumentException("condition is not FileCondition", nameof(condition));

        var fileSystem = services.GetRequiredService<IFileSystem>();
        var variableResolver = services.GetRequiredService<IVariableResolver>();

        var filePath = fileCondition.FilePath;
        filePath = variableResolver.ResolveVariables(filePath, properties);
        if (string.IsNullOrEmpty(filePath) || !fileSystem.File.Exists(filePath))
            return false;
        if (fileCondition.IntegrityInformation.HashType != HashType.None)
        {
            var hashingService = services.GetRequiredService<IHashingService>();
            if (!EvaluateFileHash(hashingService, fileSystem.FileInfo.New(filePath),
                    fileCondition.IntegrityInformation.HashType, fileCondition.IntegrityInformation.Hash))
                return false;
        }

        return fileCondition.Version == null || EvaluateFileVersion(filePath, fileCondition.Version);
    }

    private static bool EvaluateFileHash(IHashingService hashingService, IFileInfo file, HashType hashType, byte[] expectedHash)
    {
        var actualHash = hashingService.GetFileHash(file, hashType);
        return actualHash.SequenceEqual(expectedHash);
    }

    private static bool EvaluateFileVersion(string filePath, Version version)
    {
        try
        {
            var fileVersion = FileVersionInfo.GetVersionInfo(filePath).FileVersion;
            if (string.IsNullOrEmpty(fileVersion))
                return false;
            return Version.Parse(fileVersion).Equals(version);
        }
        catch (Exception)
        {
            return false;
        }
    }
}