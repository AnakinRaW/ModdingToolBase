using System;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Tools;

internal sealed class RestartTool(ExternalRestartOptions options, IServiceProvider serviceProvider)
    : ProcessTool<ExternalRestartOptions>(options, serviceProvider)
{
    public override async Task<ExternalUpdaterResult> Run()
    {
        await WaitForProcessExitAsync();
        StartProcess(ExternalUpdaterResult.Restarted);
        return ExternalUpdaterResult.Restarted;
    }
}