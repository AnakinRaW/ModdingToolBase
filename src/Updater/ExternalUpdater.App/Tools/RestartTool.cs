using System;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Tools;

internal sealed class RestartTool(ExternalRestartOptions options, IServiceProvider serviceProvider)
    : ProcessTool<ExternalRestartOptions>(options, serviceProvider)
{
    public override async Task<ExternalUpdaterResult> Run()
    {
        if (string.IsNullOrEmpty(Options.AppToStart))
            throw new InvalidOperationException("The 'restart' verb requires --appToStart to be set.");

        await WaitForProcessExitAsync();
        StartProcess(ExternalUpdaterResult.Restarted);
        return ExternalUpdaterResult.Restarted;
    }
}