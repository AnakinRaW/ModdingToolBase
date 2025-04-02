using System;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

public sealed class UpdateProgressEventArgs : EventArgs
{
    public required string Component { get; init; }

    public required double Progress { get; init; }

    public required ProgressType Type { get; init; }

    public required ComponentProgressInfo DetailedProgress { get; init; }
}