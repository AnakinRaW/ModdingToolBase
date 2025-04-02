using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using System.Collections.Generic;
namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal abstract class ComponentAggregatedProgressReporter(
    IComponentProgressReporter progressReporter,
    IEnumerable<IComponentStep> steps)
    : AggregatedProgressReporter<IComponentStep, ComponentProgressInfo>(progressReporter, steps,
        ComponentStepComparer.Default)
{
    protected override string GetProgressText(IComponentStep step, string progressText)
    {
        return step.Component.GetDisplayName();
    }
}