using System;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Tools;

internal sealed class RestartTool(RestartOptions options, IServiceProvider serviceProvider)
    : ProcessTool<RestartOptions>(options, serviceProvider)
{
    public override async Task<ExternalUpdaterResult> Run()
    {
        await WaitForProcessExitAsync();
        StartProcess(ExternalUpdaterResult.Restarted);
        return ExternalUpdaterResult.Restarted;
    }
}