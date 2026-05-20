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

    public void EnsureMatchesAny(IFileInfo updater, IReadOnlyCollection<ComponentIntegrityInformation> acceptable)
    {
        if (updater is null)
            throw new ArgumentNullException(nameof(updater));
        if (acceptable is null)
            throw new ArgumentNullException(nameof(acceptable));

        if (!updater.Exists)
            throw new FileNotFoundException("External updater binary not found.", updater.FullName);

        var enforceable = new List<ComponentIntegrityInformation>();
        var hasPolicyWaiver = false;
        foreach (var i in acceptable)
        {
            if (i.HashType == HashTypeKey.None || i.Hash is null)
                hasPolicyWaiver = true;
            else
                enforceable.Add(i);
        }

        if (enforceable.Count > 0)
        {
            using var stream = updater.Open(FileMode.Open, FileAccess.Read);
            foreach (var group in enforceable.GroupBy(i => i.HashType))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var actual = _hashingService.GetHash(stream, group.Key);
                foreach (var integrity in group)
                {
                    if (actual.SequenceEqual(integrity.Hash!))
                        return;
                }
            }
        }

        if (hasPolicyWaiver || acceptable.Count == 0)
        {
            _logger?.LogWarning(
                "External updater at '{Path}' not verified against any enforced hash; a trust source waives integrity (signing not required by policy). Launching.",
                updater.FullName);
            return;
        }

        throw new SecurityException(
            $"External updater bytes at '{updater.FullName}' match none of the trusted hashes from the bundled provider or the pending update manifest. Refusing to launch.");
    }
}
