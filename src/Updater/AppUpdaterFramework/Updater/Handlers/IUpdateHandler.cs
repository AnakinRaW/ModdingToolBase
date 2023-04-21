using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Updater.Handlers;

public interface IUpdateHandler
{
    bool IsUpdating { get; }

    Task UpdateAsync(IUpdateCatalog parameter);
}