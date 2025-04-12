using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public static class Extensions
{
    public static IProductComponentIdentity ToIdentity(this IProductComponentIdentity component)
    {
        return component is ProductComponentIdentity ? component : new ProductComponentIdentity(component.Id, component.Version);
    }
}