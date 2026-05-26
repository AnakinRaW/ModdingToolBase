namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal interface ILockedFileHandler
{
    Result Handle(string file);

    public enum Result
    {
        Unlocked,
        Locked,
        RequiresRestart
    }
}