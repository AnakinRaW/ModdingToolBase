using System;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class OutOfDiskspaceException(string message) : Exception(message)
{
    public OutOfDiskspaceException() : this(nameof(OutOfDiskspaceException)) {
    }
}