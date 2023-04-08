using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework;

public static class Converter
{
    public static AppComponent ToAppComponent(this IProductComponent component)
    {
        throw new NotImplementedException();
    }

    public static ApplicationManifest ToApplicationManifest(this IProductReference productReference,
        IEnumerable<IProductComponent> components)
    {
        var appComponents = components.Select(ToAppComponent).ToList();

        return new ApplicationManifest(
            productReference.Name,
            productReference.Branch?.Name,
            productReference.Version?.ToString(),
            appComponents);
    }
}