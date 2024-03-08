using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class UpdateSession(IProductReference product, IApplicationUpdater updater) : IUpdateSession
{
    private readonly IApplicationUpdater _updater = updater ?? throw new ArgumentNullException(nameof(updater));
    private readonly CancellationTokenSource _cts = new();

    public event EventHandler<ComponentProgressEventArgs>? DownloadProgress;
    public event EventHandler<ComponentProgressEventArgs>? InstallProgress;
    public IProductReference Product { get; } = product ?? throw new ArgumentNullException(nameof(product));

    internal async Task<UpdateResult> StartUpdate()
    {
        try
        {
            _updater.Progress += OnProgress!;
            return await _updater.UpdateAsync(_cts.Token);
        }
        finally
        {
            _updater.Progress -= OnProgress!;
        }
    }

    private void OnProgress(object sender, ComponentProgressEventArgs e)
    {
        if (e.Type.Equals(ProgressTypes.Install))
            InstallProgress?.Invoke(this, e);
        else if (e.Type.Equals(ProgressTypes.Download) || e.Type.Equals(ProgressTypes.Verify))
            DownloadProgress?.Invoke(this, e);
    }

    public void Cancel()
    {
        _cts.Cancel();
    }
}