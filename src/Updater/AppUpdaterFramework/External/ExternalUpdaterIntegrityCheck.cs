using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Security;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.External;

internal sealed class ExternalUpdaterIntegrityCheck : IExternalUpdaterIntegrityCheck
{
    private readonly IHashingService _hashingService;
    private readonly ILogger? _logger;

    public ExternalUpdaterIntegrityCheck(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ExternalUpdaterIntegrityCheck));
    }

    public void EnsureMatchesAny(Stream openHandle, IReadOnlyCollection<ComponentIntegrityInformation> integrityInformation)
    {
        if (openHandle is null)
            throw new ArgumentNullException(nameof(openHandle));
        if (integrityInformation is null)
            throw new ArgumentNullException(nameof(integrityInformation));

        var enforceable = new List<ComponentIntegrityInformation>();
        var hasPolicyWaiver = false;
        foreach (var i in integrityInformation)
        {
            if (i.HashType == HashTypeKey.None || i.Hash is null)
                hasPolicyWaiver = true;
            else
                enforceable.Add(i);
        }

        if (enforceable.Count > 0)
        {
            foreach (var group in enforceable.GroupBy(i => i.HashType))
            {
                openHandle.Seek(0, SeekOrigin.Begin);
                var actual = _hashingService.GetHash(openHandle, group.Key);
                foreach (var integrity in group)
                {
                    if (actual.SequenceEqual(integrity.Hash!))
                        return;
                }
            }
        }

        var filePath = TryGetPath(openHandle);
        
        if (hasPolicyWaiver || integrityInformation.Count == 0)
        {
            _logger?.LogWarning(
                "External updater at '{Path}' not verified against any enforced hash; a trust source waives integrity. Launching.",
                filePath);
            return;
        }

        throw new SecurityException($"External updater bytes at '{filePath}' match none of the trusted hashes. Refusing to launch.");
    }

    private static string? TryGetPath(Stream stream)
    {
        return stream switch
        {
            FileStream fileStream => fileStream.Name,
            FileSystemStream fileSystemStream => fileSystemStream.Name,
            _ => null
        };
    }
}
