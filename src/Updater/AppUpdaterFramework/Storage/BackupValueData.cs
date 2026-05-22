using System;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal sealed class BackupValueData(IFileInfo destination) : IEquatable<BackupValueData>
{
    public IFileInfo Destination { get; } = destination;

    public IFileInfo? Backup { get; init; }

    public ComponentIntegrityInformation BackupIntegrity { get; init; } = ComponentIntegrityInformation.None;

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