using System;

namespace AnakinRaW.ApplicationManifestCreator;

public class ComponentFileInformation : IEquatable<ComponentFileInformation>
{
    public required string Id { get; init; }

    public bool Equals(ComponentFileInformation? other)
    {
        if (ReferenceEquals(null, other)) 
            return false;
        if (ReferenceEquals(this, other)) 
            return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) 
            return false;
        if (ReferenceEquals(this, obj)) 
            return true;
        return obj.GetType() == GetType() && Equals((ComponentFileInformation)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}