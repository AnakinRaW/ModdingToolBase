using System;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update.External;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Interaction;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

public class AppUpdateResultHandler : UpdateResultHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUpdateConfiguration _updateConfiguration;
    private readonly IApplicationUpdaterRegistry _updaterRegistry;

    public AppUpdateResultHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _updaterRegistry = serviceProvider.GetRequiredService<IApplicationUpdaterRegistry>();
        _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
    }

    protected override async Task HandleRestartRequired()
    {
        await base.HandleRestartRequired();

        if (!_updateConfiguration.SupportsRestart)
            return;

        var updateOptions = _serviceProvider.GetRequiredService<IExternalUpdaterService>().CreateUpdateOptions();
        var updater = _serviceProvider.GetRequiredService<IExternalUpdaterService>().GetExternalUpdater();

        _updaterRegistry.ScheduleUpdate(updater, updateOptions);
    }

    protected override Task HandleFailedRestore()
    {
        _updaterRegistry.ScheduleReset();
        return base.HandleFailedRestore();
    }
}