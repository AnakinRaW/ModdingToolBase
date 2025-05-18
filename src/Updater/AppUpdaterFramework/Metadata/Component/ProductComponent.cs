using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class ProductComponent(string id, SemVersion? version) : ProductComponentIdentity(id, version)
{
    public string? Name { get; init; }
   
    public abstract ComponentType Type { get; }
    
    public DetectionState DetectedState { get; set; }

    public override string ToString()
    {
        return (!string.IsNullOrEmpty(Id) ? $"{GetUniqueId()},type={Type}" : base.ToString()) ?? GetType().Name;
    }
}