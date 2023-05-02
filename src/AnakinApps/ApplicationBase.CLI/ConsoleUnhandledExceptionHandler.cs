using System;
using AnakinRaW.ApplicationBase.Services;

namespace AnakinRaW.ApplicationBase;

internal sealed class ConsoleUnhandledExceptionHandler : UnhandledExceptionHandler
{
    public ConsoleUnhandledExceptionHandler(IServiceProvider services) : base(services)
    {
    }

    protected override void HandleGlobalException(Exception e)
    {
        Console.ReadLine();
    }
}