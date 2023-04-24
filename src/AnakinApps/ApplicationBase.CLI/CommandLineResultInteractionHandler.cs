using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Interaction;

namespace AnakinRaW.ApplicationBase;

internal class CommandLineResultInteractionHandler : IUpdateResultInteractionHandler
{
    public CommandLineResultInteractionHandler(IServiceProvider serviceProvider)
    {
    }

    public Task<bool> ShallRestart(RestartReason reason)
    {
        return Task.FromResult(true);
    }

    public Task ShowError(string message)
    {
        throw new NotImplementedException();
    }
}