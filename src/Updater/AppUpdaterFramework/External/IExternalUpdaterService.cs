using System.IO.Abstractions;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.AppUpdaterFramework.External;

public interface IExternalUpdaterService
{
    ExternalUpdateOptions CreateUpdateOptions(bool restartHost = true);

    ExternalRestartOptions CreateRestartOptions(bool elevate = false);

    IFileInfo GetExternalUpdater();

    void Launch(ExternalUpdaterOptions options);
}