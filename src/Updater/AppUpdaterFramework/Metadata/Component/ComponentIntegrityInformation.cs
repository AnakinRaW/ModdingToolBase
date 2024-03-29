using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public readonly struct ComponentIntegrityInformation(byte[]? hash, HashTypeKey hashType)
{
    public static readonly ComponentIntegrityInformation None = new(null, HashTypeKey.None);

    public byte[]? Hash { get; } = hash;

    public HashTypeKey HashType { get; } = hashType;
}