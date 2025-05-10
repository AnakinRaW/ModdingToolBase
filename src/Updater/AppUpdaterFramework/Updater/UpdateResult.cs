using System;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Restart;

namespace AnakinRaW.AppUpdaterFramework.Updater;

public sealed record UpdateResult
{
    public Exception? Exception { get; init; }

    public string? ErrorMessage => Exception is AggregateException aggregateException
        ? aggregateException.InnerExceptions.First().Message
        : Exception?.Message;

    public bool IsCanceled { get; init; }

    public bool FailedRestore { get; init; }

    public RestartType RestartType { get; init; }
}