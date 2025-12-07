using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.FtpUploader;

internal class LocalUploader(UploadOptions options, IServiceProvider services) : UploaderBase(options, services)
{
    protected override string GetBranchPath(string toolBasePath, string branchName)
    {
        return FileSystem.Path.Combine(FileSystem.Path.TrimEndingDirectorySeparator(toolBasePath), branchName);
    }

    protected override Task ConnectAsync()
    {
        // Nothing to do
        return Task.CompletedTask;
    }

    protected override void CreateFolders(string toolBasePath, string branchPath)
    {
        toolBasePath = FileSystem.Path.TrimEndingDirectorySeparator(toolBasePath);
        if (!FileSystem.Directory.Exists(toolBasePath))
            FileSystem.Directory.CreateDirectory(toolBasePath);

        if (!FileSystem.Directory.Exists(branchPath))
            FileSystem.Directory.CreateDirectory(branchPath);
        else
        {
            FileSystem.Directory.Delete(branchPath, true);
            FileSystem.Directory.CreateDirectory(branchPath);
        }
    }

    protected override async Task UploadFile(IFileInfo fileToUpload, string basePath)
    {
        basePath = FileSystem.Path.TrimEndingDirectorySeparator(basePath);
        var destPath = FileSystem.Path.Combine(basePath, fileToUpload.Name);

        Logger?.LogInformation("Creating local file '{FileName}' at {DestPath}", fileToUpload.Name, destPath);

        var destDir = FileSystem.Path.GetDirectoryName(destPath);
        if (destDir is not null && !FileSystem.Directory.Exists(destDir))
            FileSystem.Directory.CreateDirectory(destDir);

        await using var srcStream = fileToUpload.OpenRead();
        await using var destStream = FileSystem.File.Create(destPath);
        await srcStream.CopyToAsync(destStream);
    }

    protected override Task DisconnectAsync()
    {
        // Nothing to do
        return Task.CompletedTask;
    }
}
