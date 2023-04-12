using System;
using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Storage;

public class BackupValueData : IEquatable<BackupValueData>
{
    public IFileInfo Destination { get; }

    public IFileInfo? Backup { get; }

    public BackupValueData(IFileInfo destination)
    {
        Destination = destination;
        Backup = null;
    }

    public BackupValueData(IFileInfo destination, IFileInfo backup)
    {
        Destination = destination;
        Backup = backup;
    }

    public bool IsOriginallyMissing()
    {
        return Backup is null;
    }

    public bool Equals(BackupValueData? other)
    {
        return Destination.Equals(other?.Destination) && Equals(Backup, other.Backup);
    }

    public override bool Equals(object? obj)
    {
        return obj is BackupValueData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Destination, Backup);
    }
}