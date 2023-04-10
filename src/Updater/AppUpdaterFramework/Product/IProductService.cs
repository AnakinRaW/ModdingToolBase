using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Product;

public interface IProductService
{
    IInstalledProduct GetCurrentInstance();

    IProductReference CreateProductReference(SemVersion? newVersion, ProductBranch? branch);

    IInstalledComponentsCatalog GetInstalledComponents();
}