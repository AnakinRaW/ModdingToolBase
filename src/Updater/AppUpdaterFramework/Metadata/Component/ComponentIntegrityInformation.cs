using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public readonly struct ComponentIntegrityInformation(byte[]? hash, HashType hashType)
{
    public static readonly ComponentIntegrityInformation None = new(null, HashType.None);

    public byte[]? Hash { get; } = hash;

    public HashType HashType { get; } = hashType;
}