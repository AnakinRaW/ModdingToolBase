using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Product;

public interface IProductService
{
    InstalledProduct GetCurrentInstance();

    ProductReference CreateProductReference(SemVersion? newVersion, ProductBranch? branch);

    void UpdateComponentDetectionState();
}