using System.IO.Abstractions;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ApplicationBase.Services;

public interface IApplicationUpdaterRegistry
{
    bool Reset { get; }

    bool RequiresUpdate { get; }

    string? UpdateCommandArgs { get; }

    string? UpdaterPath { get; }

    void ScheduleReset();

    void Clear();

    void ScheduleUpdate(IFileInfo updater, ExternalUpdaterOptions options);
}