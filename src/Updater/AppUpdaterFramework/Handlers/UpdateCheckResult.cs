using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public sealed class UpdateCheckResult(IUpdateCatalog? updateCatalog)
{
    public IUpdateCatalog? UpdateCatalog { get; } = updateCatalog;

    public bool IsUpdateAvailable => UpdateCatalog is not null && UpdateCatalog.Action != UpdateCatalogAction.None;
}