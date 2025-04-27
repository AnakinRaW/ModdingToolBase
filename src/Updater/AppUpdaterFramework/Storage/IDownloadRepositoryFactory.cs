namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IDownloadRepositoryFactory
{
    IReadOnlyFileRepository GetReadOnlyRepository();

    IFileRepository GetRepository();
}