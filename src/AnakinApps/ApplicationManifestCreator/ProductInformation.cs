namespace AnakinRaW.ApplicationManifestCreator;

public class ProductInformation
{
    public string Name { get; }
    public string? Version { get; }
    public string? Branch { get; }

    public ProductInformation(string name, string version, string branch)
    {
        Name = name;
        Version = version;
        Branch = branch;
    }
}