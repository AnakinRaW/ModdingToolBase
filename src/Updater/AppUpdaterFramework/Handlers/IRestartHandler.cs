using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public interface IRestartHandler
{
    void Restart(RequiredRestartOptionsKind restartKind);
}