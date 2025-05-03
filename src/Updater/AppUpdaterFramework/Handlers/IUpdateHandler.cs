using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public interface IUpdateHandler
{
    bool IsUpdating { get; }

    bool IsCheckingForUpdate { get; }

    Task<UpdateCheckResult> CheckForUpdateAsync(IProductReference productReference, CancellationToken token = default);

    Task UpdateAsync(IUpdateCatalog parameter);
}