using System;

namespace AnakinRaW.AppUpdaterFramework.Restart;

internal class ElevationRequiredException : Exception
{
    public override string Message => "The application is required to with elevated privileges.";
}