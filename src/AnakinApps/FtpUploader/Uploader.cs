using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework;
using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using IServiceProvider = System.IServiceProvider;

namespace AnakinRaW.FtpUploader;

internal class Uploader : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly SftpClient _sftpClient;
    private readonly FtpUploadOptions _options;

    public Uploader(FtpUploadOptions options, IServiceProvider services)
    {
        _services = services;
        _fileSystem = services.GetRequiredService<IFileSystem>();
        _options = options;
        _sftpClient = new SftpClient(_options.Host, _options.Port, _options.UserName, _options.Password);
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public async Task Run()
    {
        var fileInformation = GetFileInformation();

        var branchName = await GetBranchName(fileInformation.Manifest);

        var toolBasePath = _options.BasePath;
        var branchPath = Url.Combine(toolBasePath, branchName);
        
        _sftpClient.Connect();
        
        CreateFolders(toolBasePath, branchPath);


        await UploadFile(fileInformation.Manifest, branchPath);

        if (fileInformation.BranchLookup is not null)
            await UploadFile(fileInformation.BranchLookup, toolBasePath);

        foreach (var applicationFile in fileInformation.ApplicationFiles) 
            await UploadFile(applicationFile, branchPath);
    }

    private void CreateFolders(string toolBasePath, string branchPath)
    {
        if (!_sftpClient.Exists(toolBasePath))
            _sftpClient.CreateDirectory(toolBasePath);

        if (!_sftpClient.Exists(branchPath))
            _sftpClient.CreateDirectory(branchPath);
        else
        {
            DeleteDirectoryRecursive(branchPath);
            _sftpClient.CreateDirectory(branchPath);
        }
    }

    private void DeleteDirectoryRecursive(string path)
    {
        foreach (var file in _sftpClient.ListDirectory(path))
        {
            if (file.Name != "." && (file.Name != ".."))
            {
                if (file.IsDirectory)
                    DeleteDirectoryRecursive(file.FullName);
                else
                    _sftpClient.DeleteFile(file.FullName);
            }
        }
        _sftpClient.DeleteDirectory(path);
    }

    private async Task UploadFile(IFileInfo fileToUpload, string basePath)
    {
        basePath = _fileSystem.Path.TrimEndingDirectorySeparator(basePath);

        // We cannot use Url.Combine (cause of unwanted encoding of space character) or Path.Combine (cause of using backslash)
        // Is this a security issue???
        var filePath = $"{basePath}/{fileToUpload.Name}";

        await using var fileStream = fileToUpload.OpenRead();
        _logger?.LogInformation($"Uploading file '{fileToUpload.Name}' to {filePath}");
        await Task.Factory.FromAsync(_sftpClient.BeginUploadFile(fileStream, filePath), _sftpClient.EndUploadFile);
    }

    private UploaderFileInformation GetFileInformation()
    {
        var sourceDirectory = _fileSystem.DirectoryInfo.New(_options.SourcePath);
        
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
        var manifest = await new JsonManifestLoader(_services).DeserializeAsync(fileStream).ConfigureAwait(false);
        return manifest is null
            ? throw new InvalidOperationException("Unable to deserialize manifest")
            : manifest.Branch ?? throw new InvalidOperationException("Manifest should contain branch name");
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            _sftpClient.Disconnect();
            _sftpClient.Dispose();
            return ValueTask.CompletedTask;
        }
        catch (Exception e)
        {
            return ValueTask.FromException(e);
        }
    }
}