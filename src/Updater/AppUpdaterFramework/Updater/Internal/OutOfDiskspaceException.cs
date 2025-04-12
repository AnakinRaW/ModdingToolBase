using System;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class OutOfDiskSpaceException(string message) : Exception(message);