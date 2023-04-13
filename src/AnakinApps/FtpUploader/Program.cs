// See https://aka.ms/new-console-template for more information

using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Debug;
using Renci.SshNet;

namespace AnakinRaW.FtpUploader;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"Raw Command line: {Environment.CommandLine}");
        return await Parser.Default.ParseArguments<FtpUploadOptions>(args)
            .MapResult(UploadFiles, ErrorArgs);
    }

    private static Task<int> ErrorArgs(IEnumerable<Error> arg)
    {
        return Task.FromResult(0xA0);
    }

    private static async Task<int> UploadFiles(FtpUploadOptions opts)
    {
        var services = CreateServices(opts);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
        try
        {
            var result = await new Uploader(opts, services).Run();
            logger?.LogTrace($"Uploader finished with result: {result}");
            return 0;
        }
        catch (Exception e)
        {
            logger?.LogCritical(e, e.Message);
            return e.HResult;
        }
    }

    private static IServiceProvider CreateServices(FtpUploadOptions options)
    {
        var services = new ServiceCollection();
        var fileSystem = new FileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);
        
        services.AddLogging(l =>
        {
            l.ClearProviders();
#if DEBUG
            l.AddConsole().SetMinimumLevel(LogLevel.Trace);
            l.AddDebug().SetMinimumLevel(LogLevel.Trace);
#endif
        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
        });

        return services.BuildServiceProvider();
    }
}

internal class Uploader
{
    public FtpUploadOptions Options { get; }

    public Uploader(FtpUploadOptions options, IServiceProvider services)
    {
        Options = options;
    }

    public async Task<object> Run()
    {
        var c = new SftpClient("republicatwar.com", 1579, "***", "***");

        c.Connect();

        c.CreateDirectory("downloads/TestTool");

        return 0;
    }
}

internal class FtpUploadOptions
{
    [Option('h', "host", Required = true, HelpText = "The host url.")]
    public required string Host { get; init; }

    [Option("port", Required = false, Default = 22, HelpText = "The port of the SFTP instance.")]

    public required int Port { get; init; }

    [Option('u', "user", Required = true, HelpText = "The user name to login to the SFTP instance.")]
    public required string UserName { get; init; }

    [Option('p', "password", Required = false, HelpText = "The password to authenticate the user.")]

    public required string Password { get; init; } = string.Empty;

    [Option("base", Required = true, HelpText = "The base path where file shall get uploaded too.")]
    public required string BasePath { get; init; }

    [Option("branch", Required = true, HelpText = "The name of the branch for the application to upload files for.")]
    public required string BranchName { get; init; }

    [Option('s', "source", Required = true, HelpText = "The source path where all application files are located for uploading, including the manifest and optional branch lookup file.")]
    public required string SourcePath { get; init; }
}