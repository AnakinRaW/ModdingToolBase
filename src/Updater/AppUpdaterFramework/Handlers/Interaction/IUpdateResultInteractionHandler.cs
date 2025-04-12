using System.Threading.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

public interface IUpdateResultInteractionHandler
{
    Task<bool> ShallRestart(RestartReason reason);

    Task ShowError(string message);
}