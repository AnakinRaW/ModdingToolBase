using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

public sealed class UpdateItem
{
    public string ComponentId { get; }

    public InstallableComponent? InstalledComponent { get; }

    public InstallableComponent? UpdateComponent { get; }

    public UpdateAction Action { get; }

    internal UpdateItem(InstallableComponent? installedComponent, InstallableComponent? updateComponent, UpdateAction action)
    {
        if (installedComponent is null && updateComponent is null)
            throw new InvalidOperationException("Cannot create update item from no component information.");
        if (installedComponent is not null && updateComponent is not null &&
            !ProductComponentIdentityComparer.VersionIndependent.Equals(installedComponent, updateComponent))
        {
            throw new InvalidOperationException(
                $"Cannot get action from not-matching product components {installedComponent.Id}:{updateComponent.Id}");
        }

        ComponentId = installedComponent?.Id ?? updateComponent!.GetUniqueId();
        InstalledComponent = installedComponent;
        UpdateComponent = updateComponent;
        Action = action;
    }

    public bool Equals(UpdateItem? other)
    {
        return ComponentId == other?.ComponentId;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is UpdateItem other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ComponentId.GetHashCode();
    }

    public override string ToString()
    {
        return
            $"Component:'{ComponentId}', Action:{Action}, InstalledComponent:{InstalledComponent}, UpdateComponent:{UpdateComponent}";
    }
}