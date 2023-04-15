using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework;
using Microsoft.Extensions.DependencyInjection;
using Renci.SshNet;

namespace AnakinRaW.FtpUploader;

internal class Uploader
{
    private readonly IServiceProvider _services;
    private readonly IFileSystem _fileSystem;

    public FtpUploadOptions Options { get; }

    public Uploader(FtpUploadOptions options, IServiceProvider services)
    {
        _services = services;
        _fileSystem = services.GetRequiredService<IFileSystem>();
        Options = options;
    }

    public async Task<object> Run()
    {

        var fileInformation = GetFileInformation();

        var branchName = await GetBranchName(fileInformation.Manifest);

        using var c = new SftpClient(Options.Host, Options.Port, Options.UserName, Options.Password);

        c.Connect();

        c.CreateDirectory("downloads/TestTool");

        return 0;
    }

    private UploaderFileInformation GetFileInformation()
    {
        var sourceDirectory = _fileSystem.DirectoryInfo.New(Options.SourcePath);
        
        var manifest = sourceDirectory.GetFiles(ApplicationConstants.ManifestFileName).FirstOrDefault();
        if (manifest is null)
            throw new InvalidOperationException("Unable to find manifest.json");
       
        var branchLookup = sourceDirectory.GetFiles(ApplicationConstants.BranchLookupFileName).FirstOrDefault();

        var appFiles = sourceDirectory.GetFiles("*.*")
            .Where(f => f.Name is not ApplicationConstants.ManifestFileName and not ApplicationConstants.ManifestFileName);

        return new UploaderFileInformation(manifest, appFiles, branchLookup);
    }

    private async Task<string> GetBranchName(IFileInfo manifestFile)
    {
        await using var fileStream = manifestFile.OpenRead();
        var manifest = await new JsonManifestLoader(_services).DeserializeAsync(fileStream);
        return manifest is null
            ? throw new InvalidOperationException("Unable to deserialize manifest")
            : manifest.Branch ?? ApplicationConstants.StableBranchName;
    }
}