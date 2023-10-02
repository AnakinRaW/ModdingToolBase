using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Updater;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public interface IUpdateResultHandler
{
    Task Handle(UpdateResult result);
}