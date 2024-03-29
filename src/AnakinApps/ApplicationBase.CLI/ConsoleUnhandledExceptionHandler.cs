using System;
using AnakinRaW.ApplicationBase.Services;

namespace AnakinRaW.ApplicationBase;

internal sealed class ConsoleUnhandledExceptionHandler(IServiceProvider services) : UnhandledExceptionHandler(services)
{
    protected override void HandleGlobalException(Exception e)
    {
    }
}