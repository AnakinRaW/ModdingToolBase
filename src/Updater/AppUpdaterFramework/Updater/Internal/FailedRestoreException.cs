using System;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class FailedRestoreException : UpdateException
{
    public override string Message => $"Reset failed: {InnerException}";

    public FailedRestoreException(Exception innerException) : base("UpdateAsync restore failed", innerException)
    {
        Requires.NotNull(innerException, nameof(innerException));
    }
}