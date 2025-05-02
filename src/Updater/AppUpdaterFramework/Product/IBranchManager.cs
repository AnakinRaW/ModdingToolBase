using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Product;

public interface IBranchManager
{
    string StableBranchName { get; }

    ProductBranch GetBranchFromName(string branchName);

    Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync();

    Task<IProductManifest> GetManifestAsync(IProductReference branch, CancellationToken token = default);
}