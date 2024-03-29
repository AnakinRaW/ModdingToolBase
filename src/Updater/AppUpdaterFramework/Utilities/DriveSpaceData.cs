using System;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal class DriveSpaceData(long currentInstallSize, string driveName) : IEquatable<DriveSpaceData>
{
    public long RequestedSize { get; set; } = currentInstallSize;

    public bool HasEnoughDiskSpace { get; set; }

    public long AvailableDiskSpace { get; set; }

    public string DriveName { get; } = driveName;


    public bool Equals(DriveSpaceData? other)
    {
        return string.Equals(DriveName, other?.DriveName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return DriveName.ToLowerInvariant().GetHashCode();
    }
}