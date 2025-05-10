using System;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Tools;

internal class ToolFactory
{
    public ITool Create(ExternalUpdaterOptions options, IServiceProvider serviceProvider)
    {
        return options switch
        {
            ExternalUpdateOptions updateArguments => new UpdateTool(updateArguments, serviceProvider),
            ExternalRestartOptions restartArguments => new RestartTool(restartArguments, serviceProvider),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}