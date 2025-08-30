using AnakinRaW.CommonUtilities.DownloadManager.Validation;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

public interface IManifestValidatorProvider
{
    IDownloadValidator GetValidator();
}