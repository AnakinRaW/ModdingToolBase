namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

public struct ComponentProgressInfo
{
    public int CurrentComponent { get; internal init; }

    public int TotalComponents { get; internal init; }

    public long DownloadedSize { get; internal set; }

    public long TotalSize { get; internal set; }

    public long DownloadSpeed { get; internal set; }

    public override string ToString() =>
        $"Component={CurrentComponent},TotalComponents={TotalComponents},DownloadedSize={DownloadedSize},Total={TotalSize},DownloadSpeed={DownloadSpeed}";
}