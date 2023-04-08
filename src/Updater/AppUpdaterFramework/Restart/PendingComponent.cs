using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public record PendingComponent
{
    public required IInstallableComponent Component { get; init; }

    public required UpdateAction Action { get; init; }
}