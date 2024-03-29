using System;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class FailedRestoreException : Exception
{
    public override string Message => $"Reset failed: {InnerException}";

    public FailedRestoreException(Exception innerException) : base("Update restore failed", innerException)
    {
        if (innerException == null) 
            throw new ArgumentNullException(nameof(innerException));
    }
}