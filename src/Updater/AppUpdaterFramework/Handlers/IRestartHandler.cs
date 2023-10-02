using AnakinRaW.AppUpdaterFramework.Interaction;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public interface IRestartHandler
{
    void Restart(RequiredRestartOptionsKind restartKind);
}