using System;
using System.Windows.Input;
using System.Windows.Threading;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class CommandUpdateHandler : UpdateHandler
{
    private readonly IAppDispatcher _dispatcher;

    public CommandUpdateHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _dispatcher = serviceProvider.GetRequiredService<IAppDispatcher>();
    }

    protected override void OnUpdateCompleted(object sender, EventArgs e)
    {
        base.OnUpdateCompleted(sender, e);
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }

    protected override void OnUpdateStarted(object sender, IUpdateSession e)
    {
        base.OnUpdateStarted(sender, e);
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }
}