using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class AggregatedInstallProgressReporter(IComponentProgressReporter progressReporter, IEnumerable<IComponentStep> steps)
    : ComponentAggregatedProgressReporter(progressReporter, steps)
{
    private readonly object _syncLock = new();
    private readonly HashSet<string> _visitedComponents = new();

    private long _totalProgressSize;

    protected override ProgressType Type => ProgressTypes.Install;

    protected override double CalculateAggregatedProgress(IComponentStep step, double taskProgress, out ComponentProgressInfo progressInfo)
    {
        lock (_syncLock)
        {
            _visitedComponents.Add(step.Component.GetUniqueId());
            var totalTaskProgressSize = (long)(taskProgress * step.Size);
            _totalProgressSize += totalTaskProgressSize;
            var totalProgress = (double)_totalProgressSize / TotalSize;
            totalProgress = Math.Min(totalProgress, 1.0);
            totalProgress = totalProgress >= 1.0 ? 0.99 : totalProgress;

            progressInfo = new()
            {
                TotalComponents = TotalStepCount,
                CurrentComponent = _visitedComponents.Count
            };
            return totalProgress;
        }
    }
}