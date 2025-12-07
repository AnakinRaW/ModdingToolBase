using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ExternalUpdater.Utilities;

internal class ExternalUpdater
{
    private readonly ILogger? _logger;

    private readonly IFileSystem _fileSystem;

    // Layout - Key: Destination, Value: Source
    private readonly Dictionary<string, string?> _backups;

    private IReadOnlyCollection<UpdateInformation> UpdaterItems { get; }

    public ExternalUpdater(IReadOnlyCollection<UpdateInformation> updaterItems, IServiceProvider serviceProvider)
    {
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        var json = JsonSerializer.Serialize(updaterItems, new JsonSerializerOptions { WriteIndented = true });
        _logger?.LogTrace("JSON file to process:\r\n{Json}", json);

        UpdaterItems = updaterItems;

        _backups = updaterItems.Where(x => x.Backup != null)
            .Select(x => x.Backup!)
            .ToDictionary(x => x.Destination, x => x.Source);

        foreach (var pair in _backups)
            _logger?.LogTrace("Source added: {Backup}", pair);
    }

    public ExternalUpdaterResult Run()
    {
        try
        {
            var itemsToUpdate = UpdaterItems.Where(u => u.Update is not null).Select(x => x.Update!);
            foreach (var item in itemsToUpdate)
            {
                _logger?.LogTrace("Processing item: {File}", item);
                var fileInfo = _fileSystem.FileInfo.New(item.File);

                if (string.IsNullOrEmpty(item.Destination))
                    fileInfo.DeleteWithRetry();
                else
                    fileInfo.MoveToEx(item.Destination!, true);
            }
            return ExternalUpdaterResult.UpdateSuccess;
        }
        catch (Exception e)
        {
            _logger?.LogCritical("Error while updating: {Message}", e.Message);
            _logger?.LogDebug("Restore backup");

            try
            {
                foreach (var backup in _backups)
                {
                    _logger?.LogDebug("Restore item: {Backup}", backup);
                    if (string.IsNullOrEmpty(backup.Value))
                        _fileSystem.FileInfo.New(backup.Key).DeleteWithRetry();
                    else
                        _fileSystem.FileInfo.New(backup.Value!).MoveToEx(backup.Key, true);
                }
                return ExternalUpdaterResult.UpdateFailedWithRestore;
            }
            catch (Exception backupException)
            {
                _logger?.LogError("Error while restoring backup: {Message}", backupException.Message);
                return ExternalUpdaterResult.UpdateFailedNoRestore;
            }
        }
        finally
        {
            Clean();
        }
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
}