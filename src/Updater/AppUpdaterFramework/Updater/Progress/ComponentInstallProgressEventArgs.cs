using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class ComponentInstallProgressEventArgs(double progress, string progressText) 
    : ProgressEventArgs<ComponentProgressInfo>(progress, progressText);