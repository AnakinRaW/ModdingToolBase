using System;
using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class UpdateCommand : CommandDefinition
{
    public UpdateCatalog UpdateCatalog { get; }

    public override ImageKey Image => default;

    public override ICommand Command { get; }

    public override string Text { get; }

    public override string? Tooltip => null;

    public UpdateCommand(UpdateCatalog updateCatalog, IServiceProvider serviceProvider, bool isRepair)
    {
        var handler = serviceProvider.GetRequiredService<IUpdateHandler>();

        Command = new AsyncRelayCommand(() => handler.UpdateAsync(updateCatalog), () => !handler.IsUpdating);

        UpdateCatalog = updateCatalog;
        Text = isRepair ? "Repair" : "Update";
    }
}
