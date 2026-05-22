using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater.Models;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Test.Integration;

internal sealed class IntegrationFixture : IDisposable
{
    private const string UpdaterFileName = "AnakinRaW.ExternalUpdater.exe";
    private static readonly TimeSpan UpdaterTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MarkerWaitTimeout = TimeSpan.FromSeconds(5);

    private readonly IFileSystem _fileSystem;
    private readonly IHashingService _hashing;
    private readonly string _updaterExe;
    private readonly string _workDir;
    private readonly string _appCmd;
    private readonly string _markerPath;

    public IntegrationFixture(IServiceProvider serviceProvider)
    {
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _hashing = serviceProvider.GetRequiredService<IHashingService>();
        _updaterExe = ResolveUpdaterExe();
        _workDir = CreateWorkDir(_fileSystem);
        _markerPath = _fileSystem.Path.Combine(_workDir, "marker.txt");
        _appCmd = WriteAppToStartStub(_fileSystem, _workDir, _markerPath);
    }

    public string WriteFile(string name, byte[] bytes)
    {
        var path = _fileSystem.Path.Combine(_workDir, name);
        _fileSystem.File.WriteAllBytes(path, bytes);
        return path;
    }

    public string PathInWorkDir(string name)
    {
        return _fileSystem.Path.Combine(_workDir, name);
    }

    public IntegrityInformation Sha256IntegrityOf(string filePath)
    {
        return IntegrityOf(filePath, HashTypeKey.SHA256);
    }

    public IntegrityInformation IntegrityOf(string filePath, HashTypeKey hashType)
    {
        return new IntegrityInformation { HashType = hashType.Name, Hash = Hex(filePath, hashType) };
    }

    private string Hex(string filePath, HashTypeKey hashType)
    {
        var hash = _hashing.GetHash(_fileSystem.FileInfo.New(filePath), hashType);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public bool FileExists(string path)
    {
        return _fileSystem.File.Exists(path);
    }

    public byte[] ReadAllBytes(string path)
    {
        return _fileSystem.File.ReadAllBytes(path);
    }

    public UpdateInformation MoveEntry(string source, string dest)
    {
        return new UpdateInformation
        {
            Update = new FileCopyInformation
            {
                File = source,
                Destination = dest,
                Integrity = Sha256IntegrityOf(source),
            },
        };
    }

    public UpdateInformation DeleteEntry(string file)
    {
        return new UpdateInformation
        {
            Update = new FileCopyInformation { File = file },
        };
    }

    // Runs the staged updater synchronously, returns its exit code.
    public int RunUpdater(params UpdateInformation[] items)
    {
        return RunUpdaterCore(pid: null, appToStartArgs: null, items);
    }

    // Like RunUpdater but also forwards <paramref name="appToStartArgs"/> to the launched
    // app via --appToStartArgs (base64-encoded). The .cmd stub captures %*, so the test can
    // then assert these tokens landed in the marker via AssertAppLaunchedWith.
    public int RunUpdaterWithAppArgs(string appToStartArgs, params UpdateInformation[] items)
    {
        return RunUpdaterCore(pid: null, appToStartArgs, items);
    }

    private int RunUpdaterCore(int? pid, string? appToStartArgs, UpdateInformation[] items)
    {
        using var proc = StartUpdater(pid, appToStartArgs, items);
        if (!proc.WaitForExit((int)UpdaterTimeout.TotalMilliseconds))
        {
            try
            {
                proc.Kill();
            }
            catch
            {
                // Ignore
            }
            throw new TimeoutException($"External updater did not exit within {UpdaterTimeout.TotalSeconds:F0}s.");
        }
        return proc.ExitCode;
    }

    // Starts the updater asynchronously. Caller owns the returned Process and is responsible for WaitForExit + disposal.
    public Process StartUpdater(int? pid, params UpdateInformation[] items)
    {
        return StartUpdater(pid, appToStartArgs: null, items);
    }

    private Process StartUpdater(int? pid, string? appToStartArgs, UpdateInformation[] items)
    {
        var payload = items.ToPayload();

        var sb = new StringBuilder($"update --appToStart \"{_appCmd}\" --updatePayload {payload}");
        if (pid.HasValue)
            sb.Append($" --pid {pid.Value}");
        if (!string.IsNullOrEmpty(appToStartArgs))
        {
            var b64 = Convert.ToBase64String(Encoding.Default.GetBytes(appToStartArgs!));
            sb.Append($" --appToStartArgs {b64}");
        }

        var psi = new ProcessStartInfo(_updaterExe, sb.ToString())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        return Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null.");
    }

    // Asserts the appToStart stub ran, the marker carries the expected ExternalUpdaterResult,
    // and (optionally) that each <paramref name="appArgsMustContain"/> token appears in the
    // marker — used by tests that verify --appToStartArgs pass-through.
    public void AssertAppLaunchedWith(ExternalUpdaterResult expected, params string[] appArgsMustContain)
    {
        Assert.True(WaitForMarker(), $"AppToStart did not run within {MarkerWaitTimeout.TotalSeconds:F0}s after the updater exited.");

        var marker = _fileSystem.File.ReadAllText(_markerPath);
        Assert.Contains("--externalUpdaterResult", marker);
        Assert.Contains(expected.ToString(), marker);
        foreach (var token in appArgsMustContain)
            Assert.Contains(token, marker);
    }

    public void Dispose()
    {
        try
        {
            if (_fileSystem.Directory.Exists(_workDir))
                _fileSystem.Directory.Delete(_workDir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private bool WaitForMarker()
    {
        var deadline = DateTime.UtcNow + MarkerWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_fileSystem.File.Exists(_markerPath))
                return true;
            Thread.Sleep(50);
        }
        return _fileSystem.File.Exists(_markerPath);
    }

    private static string ResolveUpdaterExe()
    {
        var baseDir = System.IO.Path.GetDirectoryName(typeof(IntegrationFixture).Assembly.Location)
                      ?? throw new InvalidOperationException("Cannot locate test assembly directory.");
        var candidate = System.IO.Path.Combine(baseDir, UpdaterFileName);
        if (!System.IO.File.Exists(candidate))
            throw new System.IO.FileNotFoundException(
                $"Expected the external updater exe at '{candidate}'. Build ExternalUpdater.App first.",
                candidate);
        return candidate;
    }

    private static string CreateWorkDir(IFileSystem fileSystem)
    {
        var path = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), "extupdater-itest-" + Guid.NewGuid().ToString("N"));
        fileSystem.Directory.CreateDirectory(path);
        return path;
    }

    // Writes a .cmd that records the args it receives into a marker file. The updater
    // launches this as the AppToStart with --externalUpdaterResult <result> appended, so
    // the marker captures exactly what the next-launched main app would see.
    private static string WriteAppToStartStub(IFileSystem fileSystem, string workDir, string markerPath)
    {
        var cmdPath = fileSystem.Path.Combine(workDir, "app-to-start.cmd");
        
        var content =
            "@echo off" + 
            Environment.NewLine +
            $"> \"{markerPath}\" echo %*" + 
            Environment.NewLine;
        
        fileSystem.File.WriteAllText(cmdPath, content);
        return cmdPath;
    }
}
