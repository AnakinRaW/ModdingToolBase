using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using AnakinRaW.ExternalUpdater.Models;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Tools;
using AnakinRaW.ExternalUpdater.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Test;

// End-to-end tests for the `update` verb: payload is decoded, files are moved, and the app
// is launched. IProcessTools is stubbed so no real processes start during the test.
public class UpdateToolTests : TestBaseWithFileSystem
{
    private readonly IHashingService _hashing;
    private readonly RecordingProcessTools _processTools;
    private readonly string _workDir;
    private readonly string _appExePath;

    public UpdateToolTests()
    {
        _hashing = ServiceProvider.GetRequiredService<IHashingService>();
        _processTools = (RecordingProcessTools)ServiceProvider.GetRequiredService<IProcessTools>();
        _workDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        FileSystem.Directory.CreateDirectory(_workDir);

        // A stand-in for the main app exe. StartProcess checks file existence before
        // delegating to IProcessTools; the bytes never get executed.
        _appExePath = FileSystem.Path.Combine(_workDir, "app.exe");
        FileSystem.File.WriteAllBytes(_appExePath, "MZ"u8.ToArray());
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<IProcessTools>(_ => new RecordingProcessTools());
    }

    [Fact]
    public async Task Run_PayloadAppliesAndAppLaunches()
    {
        var bytes = "new-content"u8.ToArray();
        var source = WriteFile("staged.bin", bytes);
        var dest = FileSystem.Path.Combine(_workDir, "installed.bin");
        var sha = Hex(await _hashing.GetHashAsync(FileSystem.FileInfo.New(source), HashTypeKey.SHA256, TestContext.Current.CancellationToken));

        var options = MakeOptions(
            new UpdateInformation { Update = new FileCopyInformation { File = source, Destination = dest, Sha256 = sha } });

        var tool = new UpdateTool(options, ServiceProvider);
        var result = await tool.Run();

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.True(FileSystem.File.Exists(dest));
        Assert.Equal(bytes, FileSystem.File.ReadAllBytes(dest));

        // App was launched with the expected exe path and the operation result was passed
        // through so the next-launched main app can react.
        var call = Assert.Single(_processTools.StartApplicationCalls);
        Assert.Equal(_appExePath, call.Application.FullName);
        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, call.AppStartOptions.Result);
        Assert.False(call.Elevate);
    }

    [Fact]
    public async Task Run_HashMismatch_AppStillLaunchesWithFailureResult()
    {
        var source = WriteFile("staged.bin", [1, 2, 3]);
        var dest = FileSystem.Path.Combine(_workDir, "installed.bin");

        var options = MakeOptions(new UpdateInformation
        {
            Update = new FileCopyInformation { File = source, Destination = dest, Sha256 = new string('0', 64) }
        });

        var tool = new UpdateTool(options, ServiceProvider);
        var result = await tool.Run();

        // Hash mismatch aborts the batch; the staged file is untouched.
        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, result);
        Assert.False(FileSystem.File.Exists(dest));

        // The main app must still be relaunched, with the failure result so it can react.
        var call = Assert.Single(_processTools.StartApplicationCalls);
        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, call.AppStartOptions.Result);
    }

    [Fact]
    public async Task Run_EmptyPayload_LaunchesAppWithSuccess()
    {
        var options = MakeOptions();

        var tool = new UpdateTool(options, ServiceProvider);
        var result = await tool.Run();

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        var call = Assert.Single(_processTools.StartApplicationCalls);
        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, call.AppStartOptions.Result);
    }

    [Fact]
    public async Task Run_PassThroughAppArgs_ForwardedToProcessTools()
    {
        var argsBase64 = Convert.ToBase64String(Encoding.Default.GetBytes("--foo bar"));
        var options = MakeOptions() with { AppToStartArguments = argsBase64 };

        var tool = new UpdateTool(options, ServiceProvider);
        await tool.Run();

        var call = Assert.Single(_processTools.StartApplicationCalls);
        Assert.Equal(argsBase64, call.PassThroughArgsBase64);
    }

    private ExternalUpdateOptions MakeOptions(params UpdateInformation[] items)
    {
        var payload = items.Length == 0
            ? Convert.ToBase64String("[]"u8.ToArray())
            : items.ToPayload();

        return new ExternalUpdateOptions
        {
            AppToStart = _appExePath,
            Payload = payload,
            Pid = null,   // skips the wait-for-parent-exit branch
            Timeout = 5,
        };
    }

    private string WriteFile(string name, byte[] bytes)
    {
        var path = FileSystem.Path.Combine(_workDir, name);
        FileSystem.File.WriteAllBytes(path, bytes);
        return path;
    }

    private static string Hex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private sealed class RecordingProcessTools : IProcessTools
    {
        public List<StartApplicationCall> StartApplicationCalls { get; } = [];

        public void StartApplication(IFileInfo application, ExternalUpdaterResultOptions appStartOptions, string? passThroughArgsBase64, bool elevate)
        {
            StartApplicationCalls.Add(new StartApplicationCall(application, appStartOptions, passThroughArgsBase64, elevate));
        }

        public Task<bool> WaitForExitAsync(int? pid, CancellationToken token)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StartApplicationCall(
        IFileInfo application,
        ExternalUpdaterResultOptions appStartOptions,
        string? passThroughArgsBase64,
        bool elevate)
    {
        public IFileInfo Application { get; } = application;
        public ExternalUpdaterResultOptions AppStartOptions { get; } = appStartOptions;
        public string? PassThroughArgsBase64 { get; } = passThroughArgsBase64;
        public bool Elevate { get; } = elevate;
    }
}
