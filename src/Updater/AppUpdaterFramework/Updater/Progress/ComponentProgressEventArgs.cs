using AnakinRaW.CommonUtilities.SimplePipeline.Progress;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class ComponentProgressEventArgs(string progressText, double progress) 
    : ProgressEventArgs<ComponentProgressInfo>(progressText, progress);