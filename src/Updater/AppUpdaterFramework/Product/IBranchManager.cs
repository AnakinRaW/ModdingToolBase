using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Product;

public interface IBranchManager
{
    string StableBranchName { get; }

    Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync();

    ProductBranch GetBranchFromVersion(SemVersion version);

    Task<IProductManifest> GetManifestAsync(IProductReference branch, CancellationToken token = default);
}