using System;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ExternalUpdater.Tools;

internal sealed class UpdateTool(UpdateOptions options, IServiceProvider serviceProvider)
    : ProcessTool<UpdateOptions>(options, serviceProvider)
{
    public override async Task<ExternalUpdaterResult> Run()
    {
        try
        {
            await WaitForProcessExitAsync().ConfigureAwait(false);
            var updateItems = await Options.GetUpdateInformationAsync(ServiceProvider).ConfigureAwait(false);
            
            var updater = new Utilities.ExternalUpdater(updateItems, ServiceProvider);
            var updateResult = await Task.Run(updater.Run).ConfigureAwait(false);
            
            Logger?.LogDebug($"Updated with result: {updateResult}");
            StartProcess(updateResult);
            return updateResult;
        }
        finally
        {
            if (!string.IsNullOrEmpty(Options.UpdateFile)) 
                FileSystem.File.Delete(Options.UpdateFile!);
        }
    }
}