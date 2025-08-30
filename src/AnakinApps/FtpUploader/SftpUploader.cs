using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Flurl;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace AnakinRaW.FtpUploader;

internal class SftpUploader(FtpUploadOptions options, IServiceProvider services) : UploaderBase(options, services)
{
    private readonly SftpClient _sftpClient = new(options.Host, options.Port, options.UserName, options.Password);

    protected override string GetBranchPath(string toolBasePath, string branchName)
    {
        return Url.Combine(toolBasePath, branchName);
    }

    protected override Task ConnectAsync()
    {
        _sftpClient.Connect();
        return Task.CompletedTask;
    }

    protected override void CreateFolders(string toolBasePath, string branchPath)
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
            if (file.Name != "." && file.Name != "..")
            {
                if (file.IsDirectory)
                    DeleteDirectoryRecursive(file.FullName);
                else
                    _sftpClient.DeleteFile(file.FullName);
            }
        }
        _sftpClient.DeleteDirectory(path);
    }

    protected override async Task UploadFile(IFileInfo fileToUpload, string basePath)
    {
        var filePath = $"{FileSystem.Path.TrimEndingDirectorySeparator(basePath)}/{fileToUpload.Name}";
        await using var fileStream = fileToUpload.OpenRead();
        Logger?.LogInformation("Uploading file '{file}' to {path}", fileToUpload.Name, filePath);
        await Task.Factory.FromAsync(_sftpClient.BeginUploadFile(fileStream, filePath), _sftpClient.EndUploadFile);
    }

    protected override Task DisconnectAsync()
    {
        try
        {
            if (_sftpClient.IsConnected)
                _sftpClient.Disconnect();
            _sftpClient.Dispose();
        }
        catch
        {
            // ignore
        }
        return Task.CompletedTask;
    }
}
