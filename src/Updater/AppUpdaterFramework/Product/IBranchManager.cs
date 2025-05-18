using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Product;

public interface IBranchManager
{
    string StableBranchName { get; }

    ProductBranch GetBranchFromName(string branchName);

    Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync();

    Task<ProductManifest> GetManifestAsync(ProductReference branch, CancellationToken token = default);
}