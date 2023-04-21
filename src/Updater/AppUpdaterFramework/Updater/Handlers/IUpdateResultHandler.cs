using System.Threading.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Updater.Handlers;

public interface IUpdateResultHandler
{
    Task Handle(UpdateResult result);
}