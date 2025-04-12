using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal class DiskSpaceCalculator(IServiceProvider serviceProvider) : IDiskSpaceCalculator
{
    private const long ExtraSizeMargin = 20_000_000;

    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly IUpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

    public void ThrowIfNotEnoughDiskSpaceAvailable(
        IInstallableComponent newComponent, 
        IInstallableComponent? oldComponent, 
        string? installPath, 
        CalculationOptions options)
    {
        if (newComponent == null) 
            throw new ArgumentNullException(nameof(newComponent));
        foreach (var diskData in GetDiskInformation(newComponent, oldComponent, installPath, options))
        {
            if (!HasEnoughSpace(diskData))
            {
                throw new OutOfDiskSpaceException(
                    $"There is not enough space for component '{newComponent.GetDisplayName()}'. {diskData.RequestedSize + ExtraSizeMargin} is required on drive {diskData.DriveName}  " +
                    $"but there is only {diskData.AvailableDiskSpace} available.");
            }
        }
    }

    private IEnumerable<DriveSpaceData> GetDiskInformation(
        IInstallableComponent newComponent, 
        IInstallableComponent? oldComponent, 
        string? installPath, 
        CalculationOptions options)
    {
        var calculatedDiskSizes = new Dictionary<string, DriveSpaceData>();

        if (options.HasFlag(CalculationOptions.Download))
        {
            var downloadRoot = _fileSystem.Path.GetPathRoot(_updateConfiguration.DownloadLocation);
            if (!string.IsNullOrEmpty(downloadRoot))
                UpdateSizeInformation(newComponent.OriginInfo?.Size, downloadRoot!);
        }

        if (options.HasFlag(CalculationOptions.Install))
        {
            var destinationRoot = _fileSystem.Path.GetPathRoot(installPath);
            if (!string.IsNullOrEmpty(destinationRoot))
                UpdateSizeInformation(newComponent.OriginInfo?.Size, destinationRoot!);
        }
        
        if (options.HasFlag(CalculationOptions.Backup) && oldComponent != null)
        {
            var backupRoot = _fileSystem.Path.GetPathRoot(_updateConfiguration.BackupLocation);
            if (!string.IsNullOrEmpty(backupRoot)) 
                UpdateSizeInformation(oldComponent.InstallationSize.Total, backupRoot!);
        }

        return calculatedDiskSizes.Values;


        void UpdateSizeInformation(long? actualSize, string drive) {
            if (!actualSize.HasValue)
                return;
            if (!calculatedDiskSizes.TryGetValue(drive, out var size))
                calculatedDiskSizes.Add(drive, new DriveSpaceData(actualSize.Value, drive));
            else
                size.RequestedSize += actualSize.Value;
        }
    }

    private bool HasEnoughSpace(DriveSpaceData spaceData)
    {
        try
        {
            var freeSpace = _fileSystem.DriveInfo.New(spaceData.DriveName).AvailableFreeSpace;
            spaceData.AvailableDiskSpace = freeSpace;
            return freeSpace >= spaceData.RequestedSize + ExtraSizeMargin;
        }
        catch
        {
            return false;
        }
    }

    [Flags]
    public enum CalculationOptions
    {
        Install = 1,
        Download = 2,
        Backup = 4,
        All = Install | Download | Backup
    }
}