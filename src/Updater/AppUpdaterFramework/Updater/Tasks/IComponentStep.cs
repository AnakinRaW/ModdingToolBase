using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline;

namespace AnakinRaW.AppUpdaterFramework.Updater.Tasks;

internal interface IComponentStep : IProgressStep<ComponentProgressInfo>
{
    ProductComponent Component { get; }
}