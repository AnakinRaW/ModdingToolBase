﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal static class Extensions
{
    internal static void RaiseAsync(this EventHandler? handler, object sender, EventArgs e)
    {
        Task.Run(() => handler?.Invoke(sender, e));
    }

    internal static void RaiseAsync<T>(this EventHandler<T>? handler, object sender, T e)
    {
        Task.Run(() => handler?.Invoke(sender, e));
    }

    public static bool IsExceptionType<T>(this Exception error) where T : Exception
    {
        switch (error)
        {
            case T:
                return true;
            case AggregateException aggregateException:
                return aggregateException.InnerExceptions.Any(p => p.IsExceptionType<T>());
            default:
                return false;
        }
    }

    internal static bool IsOperationCanceledException(this Exception error) =>
        error.IsExceptionType<OperationCanceledException>();

    public static bool IsSuccess(this InstallResult result)
    {
        return result is InstallResult.Success or InstallResult.SuccessRestartRequired;
    }

    public static bool IsFailure(this InstallResult result)
    {
        return result is InstallResult.Failure;
    }

    public static Exception? TryGetWrappedException(this Exception exception)
    {
        var wrappedExceptions = exception.TryGetWrappedExceptions();
        return wrappedExceptions is { Count: 1 } ? wrappedExceptions.Single() : null;
    }

    public static IReadOnlyCollection<Exception>? TryGetWrappedExceptions(this Exception exception)
    {
        return exception is AggregateException aggregateException ? aggregateException.Flatten().InnerExceptions : null;
    }
}