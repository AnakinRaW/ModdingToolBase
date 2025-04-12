using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.FileLocking;

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