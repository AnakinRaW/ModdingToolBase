namespace AnakinRaW.AppUpdaterFramework.Updater.Tasks;

internal enum InstallResult
{
    Success,
    SuccessRestartRequired,
    Failure,
    FailureElevationRequired,
    Cancel
}