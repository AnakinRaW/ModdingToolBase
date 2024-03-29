namespace AnakinRaW.ApplicationManifestCreator;

public class ProductInformation(string name, string version, string branch)
{
    public string Name { get; } = name;
    public string? Version { get; } = version;
    public string? Branch { get; } = branch;
}