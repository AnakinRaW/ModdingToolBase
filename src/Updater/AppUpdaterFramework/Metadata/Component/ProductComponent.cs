using System;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class ProductComponent : IProductComponent
{
    public string Id { get; }
    
    public SemVersion? Version { get; }
   
    public string? Name { get; init; }
   
    public abstract ComponentType Type { get; }
    
    public DetectionState DetectedState { get; set; }

    protected ProductComponent(IProductComponentIdentity identity)
    {
        if (identity == null) 
            throw new ArgumentNullException(nameof(identity));
        Id = identity.Id;
        Version = identity.Version;
    }

    public override string ToString()
    {
        return (!string.IsNullOrEmpty(Id) ? $"{GetUniqueId()},type={Type}" : base.ToString()) ?? GetType().Name;
    }

    public string GetUniqueId()
    {
        return ProductComponentIdentity.Format(this);
    }

    public bool Equals(IProductComponentIdentity? other)
    {
        return ProductComponentIdentityComparer.Default.Equals(this, other);
    }
}