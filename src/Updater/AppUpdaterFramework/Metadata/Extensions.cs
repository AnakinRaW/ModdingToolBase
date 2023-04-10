using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public static class Extensions
{
    public static IProductComponentIdentity ToIdentity(this IProductComponentIdentity component)
    {
        return new ProductComponentIdentity(component.Id, component.Version);
    }
}