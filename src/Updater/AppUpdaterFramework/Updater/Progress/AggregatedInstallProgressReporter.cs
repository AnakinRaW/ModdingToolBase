﻿using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class AggregatedInstallProgressReporter(IComponentProgressReporter progressReporter, IEnumerable<IComponentStep> steps)
    : ComponentAggregatedProgressReporter(progressReporter, steps)
{
    private readonly object _syncLock = new();
    private readonly HashSet<string> _visitedComponents = [];

    private long _totalProgressSize;

    protected override ProgressEventArgs<ComponentProgressInfo> CalculateAggregatedProgress(
        IComponentStep step, 
        ProgressEventArgs<ComponentProgressInfo> progress)
    {
        lock (_syncLock)
        {
            _visitedComponents.Add(step.Component.GetUniqueId());
            _totalProgressSize += (long)(progress.Progress * step.Size);
            var totalProgress = (double)_totalProgressSize / TotalSize;
            totalProgress = Math.Min(totalProgress, 1.0);
            totalProgress = totalProgress >= 1.0 ? 0.99 : totalProgress;

            var progressInfo = new ComponentProgressInfo
            {
                TotalComponents = TotalStepCount,
                CurrentComponent = _visitedComponents.Count
            };

            return new ProgressEventArgs<ComponentProgressInfo>(totalProgress, progress.ProgressText, progressInfo);
        }
    }
}