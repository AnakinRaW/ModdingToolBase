﻿using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal abstract class ComponentAggregatedProgressReporter(IComponentProgressReporter progressReporter)
    : AggregatedProgressReporter<IComponentStep, ComponentProgressInfo>(progressReporter, ComponentStepComparer.Default)
{
    protected override string GetProgressText(IComponentStep step)
    {
        return step.Component.GetDisplayName();
    }
}