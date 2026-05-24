using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal sealed class BackupValueData(string destination) : IEquatable<BackupValueData>
{
    public string Destination { get; } = destination ?? throw new ArgumentNullException(nameof(destination));

    public string? Backup { get; init; }

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