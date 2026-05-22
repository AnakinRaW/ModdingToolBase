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

    // Layout - Key: Destination, Value: Source
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
            .ToDictionary(x => x.Destination, x => new BackupEntry(x.Source, x.Sha256));

        foreach (var pair in _backups)
            _logger?.LogTrace("Backup source added: {Backup}", pair);
    }

    public ExternalUpdaterResult Run()
    {
        try
        {
            if (!ValidateSourceHashes())
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

    // Pre-validation pass: hash every source file with a declared sha256 BEFORE moving any
    // of them. Any mismatch aborts the whole batch — no partial application.
    private bool ValidateSourceHashes()
    {
        foreach (var item in UpdaterItems)
        {
            var update = item.Update;
            if (update is null)
                continue;

            // Delete entries (Destination == null) have no source bytes to verify.
            if (string.IsNullOrEmpty(update.Destination))
                continue;

            if (string.IsNullOrEmpty(update.Sha256))
            {
                _logger?.LogError(
                    "Update entry for '{Destination}' has no sha256 declaration; refusing to apply.",
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

            if (!VerifyHash(sourceFile, update.Sha256!))
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

    // Runs the restore loop. Each entry is verified before its bytes are moved over the
    // destination. Returns UpdateFailedWithRestore if every entry restored cleanly,
    // UpdateFailedNoRestore the moment any entry can't be safely restored (missing source,
    // missing/mismatched hash, or a move that itself throws).
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
        // Destination didn't exist pre-update, so restore = delete.
        if (string.IsNullOrEmpty(entry.Source))
        {
            _fileSystem.FileInfo.New(destination).DeleteWithRetry();
            return true;
        }

        if (string.IsNullOrEmpty(entry.Sha256))
        {
            _logger?.LogCritical("Backup entry for '{Destination}' declares a Source ('{Source}') but no sha256.",
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

        if (!VerifyHash(sourceFile, entry.Sha256!))
        {
            _logger?.LogCritical("Backup source hash mismatch for '{Source}' → '{Destination}'.",
                entry.Source, destination);
            return false;
        }

        sourceFile.MoveToEx(destination, true);
        return true;
    }

    private bool VerifyHash(IFileInfo file, string expectedHex)
    {
        using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        var actual = _hashing.GetHash(stream, HashTypeKey.SHA256);
        var expected = HexToBytes(expectedHex);
        return actual.SequenceEqual(expected);
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

    private readonly struct BackupEntry(string? source, string? sha256)
    {
        public string? Source { get; } = source;
        public string? Sha256 { get; } = sha256;
    }
}
