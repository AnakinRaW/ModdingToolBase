using CommandLine;

namespace AnakinRaW.FtpUploader;

internal class FtpUploadOptions
{
    [Option('h', "host", Required = true, HelpText = "The host url.")]
    public required string Host { get; init; }

    [Option("port", Required = false, Default = 22, HelpText = "The port of the SFTP instance.")]
    public required int Port { get; init; }

    [Option('u', "user", Required = true, HelpText = "The user name to login to the SFTP instance.")]
    public required string UserName { get; init; }

    [Option('p', "password", Required = false, HelpText = "The password to authenticate the SFTP user.")]
    public required string Password { get; init; } = string.Empty;

    [Option("base", Required = true, HelpText = "The base path where file shall get uploaded too.")]
    public required string BasePath { get; init; }

    [Option('s', "source", Required = true, HelpText = "The source path where all application files are located for uploading, including the manifest and optional branch lookup file.")]
    public required string SourcePath { get; init; }
}