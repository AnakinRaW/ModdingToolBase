using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IServiceProvider = System.IServiceProvider;

namespace AnakinRaW.FtpUploader;

internal abstract class UploaderBase : IAsyncDisposable
{
    protected readonly IServiceProvider Services;
    protected readonly IFileSystem FileSystem;
    protected readonly ILogger? Logger;
    protected readonly UploadOptions Options;

    protected UploaderBase(UploadOptions options, IServiceProvider services)
    {
        Services = services;
        FileSystem = services.GetRequiredService<IFileSystem>();
        Options = options;
        Logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public async Task Run()
    {
        var fileInformation = GetFileInformation();

        var branchName = await GetBranchName(fileInformation.Manifest).ConfigureAwait(false);

        var toolBasePath = Options.BasePath;
        var branchPath = GetBranchPath(toolBasePath, branchName);

        await ConnectAsync().ConfigureAwait(false);

        CreateFolders(toolBasePath, branchPath);

        await UploadFile(fileInformation.Manifest, branchPath).ConfigureAwait(false);

        if (fileInformation.BranchLookup is not null)
            await UploadFile(fileInformation.BranchLookup, toolBasePath).ConfigureAwait(false);

        foreach (var applicationFile in fileInformation.ApplicationFiles)
            await UploadFile(applicationFile, branchPath).ConfigureAwait(false);
    }

    protected abstract string GetBranchPath(string toolBasePath, string branchName);

    protected abstract Task ConnectAsync();

    protected abstract void CreateFolders(string toolBasePath, string branchPath);

    protected abstract Task UploadFile(IFileInfo fileToUpload, string basePath);

    protected abstract Task DisconnectAsync();

    private UploaderFileInformation GetFileInformation()
    {
        var sourceDirectory = FileSystem.DirectoryInfo.New(Options.SourcePath);

        var manifest = sourceDirectory.GetFiles(ApplicationConstants.ManifestFileName).FirstOrDefault();
        if (manifest is null)
            throw new InvalidOperationException("Unable to find manifest.json");

        var branchLookup = sourceDirectory.GetFiles(ApplicationConstants.BranchLookupFileName).FirstOrDefault();

        var appFiles = sourceDirectory.GetFiles("*.*")
            .Where(f => f.Name is not ApplicationConstants.ManifestFileName
                and not ApplicationConstants.BranchLookupFileName);

        return new UploaderFileInformation(manifest, appFiles, branchLookup);
    }

    private async Task<string> GetBranchName(IFileInfo manifestFile)
    {
        await using var fileStream = manifestFile.OpenRead();
        var manifest = await new JsonManifestLoader(Services).DeserializeAsync(fileStream).ConfigureAwait(false);
        return manifest is null
            ? throw new InvalidOperationException("Unable to deserialize manifest")
            : manifest.Branch ?? throw new InvalidOperationException("Manifest should contain branch name");
    }

    public virtual async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }
}