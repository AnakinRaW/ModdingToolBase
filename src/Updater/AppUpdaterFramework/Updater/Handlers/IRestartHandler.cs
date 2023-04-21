using AnakinRaW.AppUpdaterFramework.Interaction;

namespace AnakinRaW.AppUpdaterFramework.Updater.Handlers;

public interface IRestartHandler
{
    void Restart(RequiredRestartOptionsKind restartKind);
}