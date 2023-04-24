using System.IO.Abstractions;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.AppUpdaterFramework.External;

public interface IExternalUpdaterService
{
    UpdateOptions CreateUpdateOptions();

    RestartOptions CreateRestartOptions(bool elevate = false);

    IFileInfo GetExternalUpdater();

    void Launch(ExternalUpdaterOptions options);
}