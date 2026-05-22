using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ExternalUpdater.Utilities;

internal class ExternalUpdater
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IHashingService _hashing;

    private readonly Dictionary<string, BackupEntry> _backups;

    private IReadOnlyCollection<UpdateInformation> UpdaterItems { get; }

    public ExternalUpdater(IReadOnlyCollection<UpdateInformation> updaterItems, IServiceProvider serviceProvider)
    {
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _hashing = serviceProvider.GetRequiredService<IHashingService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        var json = JsonSerializer.Serialize(updaterItems, new JsonSerializerOptions { WriteIndented = true });
        _logger?.LogTrace("JSON payload to process:\r\n{Json}", json);

        UpdaterItems = updaterItems;

        _backups = updaterItems.Where(x => x.Backup != null)
            .Select(x => x.Backup!)
            .ToDictionary(x => x.Destination, x => new BackupEntry(x.Source, x.Integrity));

        foreach (var pair in _backups)
            _logger?.LogTrace("Backup source added: {Backup}", pair);
    }

    public ExternalUpdaterResult Run()
    {
        try
        {
            if (!ValidateSourceIntegrity())
                return ExternalUpdaterResult.UpdateFailedNoRestore;

            ApplyUpdates();
            return ExternalUpdaterResult.UpdateSuccess;
        }
        catch (Exception e)
        {
            _logger?.LogCritical("Error while updating: {Message}", e.Message);
            return RestoreBackups();
        }
        finally
        {
            Clean();
        }
    }

    private bool ValidateSourceIntegrity()
    {
        foreach (var item in UpdaterItems)
        {
            var update = item.Update;
            if (update is null)
                continue;

            if (string.IsNullOrEmpty(update.Destination))
                continue;

            if (update.Integrity is null)
            {
                _logger?.LogError(
                    "Update entry for '{Destination}' has no integrity declaration; refusing to apply.",
                    update.Destination);
                return false;
            }

            var sourceFile = _fileSystem.FileInfo.New(update.File);
            if (!sourceFile.Exists)
            {
                _logger?.LogError(
                    "Source file '{File}' for destination '{Destination}' not found.",
                    update.File, update.Destination);
                return false;
            }

            if (!VerifyHash(sourceFile, update.Integrity.Value))
            {
                _logger?.LogCritical(
                    "Source hash mismatch for '{File}' → '{Destination}'. Aborting batch.",
                    update.File, update.Destination);
                return false;
            }
        }
        return true;
    }

    private void ApplyUpdates()
    {
        var itemsToUpdate = UpdaterItems
            .Where(u => u.Update is not null)
            .Select(x => x.Update!);

        foreach (var item in itemsToUpdate)
        {
            _logger?.LogTrace("Processing item: {File}", item);
            var fileInfo = _fileSystem.FileInfo.New(item.File);

            if (string.IsNullOrEmpty(item.Destination))
                fileInfo.DeleteWithRetry();
            else
                fileInfo.MoveToEx(item.Destination!, true);
        }
    }

    private ExternalUpdaterResult RestoreBackups()
    {
        _logger?.LogDebug("Restoring backups");
        try
        {
            foreach (var backup in _backups)
            {
                _logger?.LogDebug("Restore item: {Backup}", backup);
                if (!RestoreOneEntry(backup.Key, backup.Value))
                    return ExternalUpdaterResult.UpdateFailedNoRestore;
            }
            return ExternalUpdaterResult.UpdateFailedWithRestore;
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error while restoring backup: {Message}", ex.Message);
            return ExternalUpdaterResult.UpdateFailedNoRestore;
        }
    }

    private bool RestoreOneEntry(string destination, BackupEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Source))
        {
            _fileSystem.FileInfo.New(destination).DeleteWithRetry();
            return true;
        }

        if (entry.Integrity is null)
        {
            _logger?.LogCritical("Backup entry for '{Destination}' declares a Source ('{Source}') but no integrity.",
                destination, entry.Source);
            return false;
        }

        var sourceFile = _fileSystem.FileInfo.New(entry.Source!);
        if (!sourceFile.Exists)
        {
            _logger?.LogCritical("Backup source '{Source}' for destination '{Destination}' not found; cannot restore.",
                entry.Source, destination);
            return false;
        }

        if (!VerifyHash(sourceFile, entry.Integrity.Value))
        {
            _logger?.LogCritical("Backup source hash mismatch for '{Source}' → '{Destination}'.",
                entry.Source, destination);
            return false;
        }

        sourceFile.MoveToEx(destination, true);
        return true;
    }

    private bool VerifyHash(IFileInfo file, IntegrityInformation integrity)
    {
        var hashType = ResolveHashType(integrity.HashType);
        if (hashType is null)
        {
            _logger?.LogCritical("Unsupported hash type '{HashType}' for '{File}'.", integrity.HashType, file.FullName);
            return false;
        }

        using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        var actual = _hashing.GetHash(stream, hashType.Value);
        var expected = HexToBytes(integrity.Hash);
        return actual.SequenceEqual(expected);
    }

    private static HashTypeKey? ResolveHashType(string name)
    {
        if (string.Equals(name, HashTypeKey.SHA256.Name, StringComparison.OrdinalIgnoreCase)) 
            return HashTypeKey.SHA256;
        if (string.Equals(name, HashTypeKey.SHA384.Name, StringComparison.OrdinalIgnoreCase))
            return HashTypeKey.SHA384;
        if (string.Equals(name, HashTypeKey.SHA512.Name, StringComparison.OrdinalIgnoreCase)) 
            return HashTypeKey.SHA512;
        if (string.Equals(name, HashTypeKey.SHA1.Name, StringComparison.OrdinalIgnoreCase)) 
            return HashTypeKey.SHA1;
        if (string.Equals(name, HashTypeKey.MD5.Name, StringComparison.OrdinalIgnoreCase))
            return HashTypeKey.MD5;
        return null;
    }

    private static byte[] HexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new FormatException($"Invalid hex string length: {hex.Length}");
        var result = new byte[hex.Length / 2];
        for (var i = 0; i < result.Length; i++)
            result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return result;
    }

    private void Clean()
    {
        try
        {
            foreach (var item in UpdaterItems)
            {
                var sourceFile = item.Update?.File;
                if (!string.IsNullOrEmpty(sourceFile))
                    _fileSystem.FileInfo.New(sourceFile!).DeleteWithRetry();

                var backup = item.Backup?.Source;
                if (!string.IsNullOrEmpty(backup))
                    _fileSystem.FileInfo.New(backup!).DeleteWithRetry();
            }
        }
        catch (Exception e)
        {
            _logger?.LogWarning("Cleaning failed with error: {Message}", e.Message);
        }
    }

    private readonly struct BackupEntry(string? source, IntegrityInformation? integrity)
    {
        public string? Source { get; } = source;
        public IntegrityInformation? Integrity { get; } = integrity;
    }
}
