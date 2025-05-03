using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal interface ILockedFileHandler
{
    Result Handle(IFileInfo file);

    public enum Result
    {
        Unlocked,
        Locked,
        RequiresRestart
    }
}