using System;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class OutOfDiskspaceException : Exception
{
    public OutOfDiskspaceException() : base(nameof(OutOfDiskspaceException)) {
    }

    public OutOfDiskspaceException(string message) : base(message) {
    }
}