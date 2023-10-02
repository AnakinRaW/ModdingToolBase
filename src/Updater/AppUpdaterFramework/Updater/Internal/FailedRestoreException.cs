using System;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class FailedRestoreException : Exception
{
    public override string Message => $"Reset failed: {InnerException}";

    public FailedRestoreException(Exception innerException) : base("Update restore failed", innerException)
    {
        Requires.NotNull(innerException, nameof(innerException));
    }
}