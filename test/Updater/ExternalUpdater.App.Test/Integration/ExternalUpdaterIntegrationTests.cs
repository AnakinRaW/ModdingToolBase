using System;
using System.Diagnostics;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using AnakinRaW.ExternalUpdater.Models;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Test.Integration;

// End-to-end tests: spawn the real AnakinRaW.ExternalUpdater.exe with a base64 payload on
// the command line, observe disk after it exits, and verify the appToStart was launched
// with the correct --externalUpdaterResult.
public class ExternalUpdaterIntegrationTests : TestBaseWithFileSystem, IDisposable
{
    private readonly IntegrationFixture _fixture;

    public ExternalUpdaterIntegrationTests()
    {
        _fixture = new IntegrationFixture(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    protected override IFileSystem CreateFileSystem()
    {
        return new RealFileSystem();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public void SingleMove_WithValidHash_FileMovedAndAppLaunchedWithSuccess()
    {
        var bytes = "payload"u8.ToArray();
        var source = _fixture.WriteFile("source.bin", bytes);
        var dest = _fixture.PathInWorkDir("dest.bin");

        var exit = _fixture.RunUpdater(_fixture.MoveEntry(source, dest));

        Assert.Equal(0, exit);
        Assert.True(_fixture.FileExists(dest), $"Destination '{dest}' should have been created.");
        Assert.Equal(bytes, _fixture.ReadAllBytes(dest));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }

    [Fact]
    public void MultipleMoves_AllAppliedAndAppLaunched()
    {
        var s1 = _fixture.WriteFile("a.bin", [0x10]);
        var s2 = _fixture.WriteFile("b.bin", [0x20, 0x21]);
        var s3 = _fixture.WriteFile("c.bin", [0x30, 0x31, 0x32]);
        var d1 = _fixture.PathInWorkDir("a-dest.bin");
        var d2 = _fixture.PathInWorkDir("b-dest.bin");
        var d3 = _fixture.PathInWorkDir("c-dest.bin");

        var exit = _fixture.RunUpdater(
            _fixture.MoveEntry(s1, d1),
            _fixture.MoveEntry(s2, d2),
            _fixture.MoveEntry(s3, d3));

        Assert.Equal(0, exit);
        Assert.True(_fixture.FileExists(d1));
        Assert.True(_fixture.FileExists(d2));
        Assert.True(_fixture.FileExists(d3));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        var target = _fixture.WriteFile("delete-me.bin", [1, 2, 3]);

        var exit = _fixture.RunUpdater(_fixture.DeleteEntry(target));

        Assert.Equal(0, exit);
        Assert.False(_fixture.FileExists(target), $"File '{target}' should have been deleted.");
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }

    [Fact]
    public void Sha256Mismatch_NoMovesAndAppLaunchedWithFailure()
    {
        var source = _fixture.WriteFile("source.bin", [1, 2, 3]);
        var dest = _fixture.PathInWorkDir("dest.bin");

        var exit = _fixture.RunUpdater(new UpdateInformation
        {
            Update = new FileCopyInformation
            {
                File = source,
                Destination = dest,
                Sha256 = new string('0', 64), // declared but won't match
            },
        });

        Assert.Equal(0, exit);
        Assert.False(_fixture.FileExists(dest));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateFailedNoRestore);
    }

    [Fact]
    public void EmptyPayload_LaunchesAppWithSuccessAndNoFileMutation()
    {
        var bystander = _fixture.WriteFile("bystander.bin", [9, 9, 9]);

        var exit = _fixture.RunUpdater();

        Assert.Equal(0, exit);
        Assert.True(_fixture.FileExists(bystander));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }

    [Fact]
    public void MoveWithBackup_RoundTripsAndAppLaunched()
    {
        var sourceBytes = "new-content"u8.ToArray();
        var source = _fixture.WriteFile("staged.bin", sourceBytes);
        var dest = _fixture.WriteFile("installed.bin", "old-content"u8.ToArray());
        var backupSource = _fixture.WriteFile("old.bak", "old-content"u8.ToArray());

        var exit = _fixture.RunUpdater(new UpdateInformation
        {
            Update = new FileCopyInformation
            {
                File = source,
                Destination = dest,
                Sha256 = _fixture.Sha256Hex(source),
            },
            Backup = new BackupInformation { Destination = dest, Source = backupSource },
        });

        Assert.Equal(0, exit);
        Assert.Equal(sourceBytes, _fixture.ReadAllBytes(dest));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }

    [Fact]
    public void MidBatchFailure_RestoresBackupAndLaunchesAppWithFailedWithRestore()
    {
        // Item 1 successfully overwrites installed.bin with NEW bytes. Item 2 then fails
        // (its destination's parent directory doesn't exist). The catch handler must move
        // the backup source over installed.bin, observable as "installed.bin is back to
        // OLD bytes" AND "the backup source no longer exists on disk".
        var oldBytes = "old-content"u8.ToArray();
        var newBytes = "new-content"u8.ToArray();

        var installedFile = _fixture.WriteFile("installed.bin", oldBytes);
        var stagedNew = _fixture.WriteFile("staged-new.bin", newBytes);
        var backupSource = _fixture.WriteFile("installed.bin.bak", oldBytes);

        var doomedSource = _fixture.WriteFile("doomed-source.bin", "doomed"u8.ToArray());
        var doomedDest = _fixture.PathInWorkDir("missing-dir/doomed-dest.bin");

        var exit = _fixture.RunUpdater(
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = stagedNew,
                    Destination = installedFile,
                    Sha256 = _fixture.Sha256Hex(stagedNew),
                },
                Backup = new BackupInformation
                {
                    Destination = installedFile,
                    Source = backupSource,
                    Sha256 = _fixture.Sha256Hex(backupSource),
                },
            },
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = doomedSource,
                    Destination = doomedDest,
                    Sha256 = _fixture.Sha256Hex(doomedSource),
                },
            });

        Assert.Equal(0, exit);

        // Without the restore, installed.bin would still contain NEW bytes from item 1.
        Assert.True(_fixture.FileExists(installedFile));
        Assert.Equal(oldBytes, _fixture.ReadAllBytes(installedFile));
        Assert.False(_fixture.FileExists(backupSource),
            "Backup source should have been consumed by the restore step.");

        // The doomed move never landed on disk.
        Assert.False(_fixture.FileExists(doomedDest));

        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateFailedWithRestore);
    }

    [Fact]
    public void BackupSourceHashMismatch_RestoreRefusedAndAppLaunchedWithFailedNoRestore()
    {
        // Item 1 succeeds (overwrites installed.bin with NEW bytes). The BackupInformation
        // declares a sha256 that does NOT match the bytes at the backup source on disk —
        // the payload's declared hash and the bytes the updater would actually copy back
        // diverge. When item 2 fails and the restore loop runs, the updater hashes the
        // backup source, sees the mismatch, refuses the restore, and returns
        // UpdateFailedNoRestore. Observable as "installed.bin still has the NEW bytes
        // from item 1" — the mismatched backup never reached the destination.
        var oldBytes = "old-content"u8.ToArray();
        var newBytes = "new-content"u8.ToArray();
        var divergentBytes = "divergent-bytes"u8.ToArray();

        var installedFile = _fixture.WriteFile("installed.bin", oldBytes);
        var stagedNew = _fixture.WriteFile("staged-new.bin", newBytes);

        // Compute the declared hash from one set of bytes…
        var bytesForHash = _fixture.WriteFile("installed.bin.bak", oldBytes);
        var declaredBackupSha = _fixture.Sha256Hex(bytesForHash);

        // …then overwrite the backup file with different bytes, so the declared hash no
        // longer matches what's on disk. The updater must catch this divergence.
        var backupSource = _fixture.WriteFile("installed.bin.bak", divergentBytes);

        var doomedSource = _fixture.WriteFile("doomed-source.bin", "doomed"u8.ToArray());
        var doomedDest = _fixture.PathInWorkDir("missing-dir/doomed-dest.bin");

        var exit = _fixture.RunUpdater(
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = stagedNew,
                    Destination = installedFile,
                    Sha256 = _fixture.Sha256Hex(stagedNew),
                },
                Backup = new BackupInformation
                {
                    Destination = installedFile,
                    Source = backupSource,
                    Sha256 = declaredBackupSha,
                }
            },
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = doomedSource,
                    Destination = doomedDest,
                    Sha256 = _fixture.Sha256Hex(doomedSource),
                }
            });

        Assert.Equal(0, exit);

        // installed.bin retains the NEW bytes from item 1 — the mismatched backup was
        // rejected and never moved over the destination.
        Assert.Equal(newBytes, _fixture.ReadAllBytes(installedFile));

        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateFailedNoRestore);
    }

    [Fact]
    public void MidBatchFailureAndBackupSourceMissing_LaunchesAppWithFailedNoRestore()
    {
        // Item 1 succeeds (overwrites installed.bin with NEW bytes) but declares a backup
        // whose Source doesn't exist. When item 2 fails and the catch tries to restore,
        // the backup move itself throws → updater returns UpdateFailedNoRestore.
        // Observable as "installed.bin still has the NEW bytes — restore never ran".
        var oldBytes = "old-content"u8.ToArray();
        var newBytes = "new-content"u8.ToArray();

        var installedFile = _fixture.WriteFile("installed.bin", oldBytes);
        var stagedNew = _fixture.WriteFile("staged-new.bin", newBytes);
        var missingBackupSource = _fixture.PathInWorkDir("ghost.bak"); // never written

        var doomedSource = _fixture.WriteFile("doomed-source.bin", "doomed"u8.ToArray());
        var doomedDest = _fixture.PathInWorkDir("missing-dir/doomed-dest.bin");

        var exit = _fixture.RunUpdater(
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = stagedNew,
                    Destination = installedFile,
                    Sha256 = _fixture.Sha256Hex(stagedNew),
                },
                Backup = new BackupInformation { Destination = installedFile, Source = missingBackupSource },
            },
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = doomedSource,
                    Destination = doomedDest,
                    Sha256 = _fixture.Sha256Hex(doomedSource),
                },
            });

        Assert.Equal(0, exit);

        // Restore never ran — installed.bin keeps the NEW bytes that item 1 wrote.
        Assert.True(_fixture.FileExists(installedFile));
        Assert.Equal(newBytes, _fixture.ReadAllBytes(installedFile));

        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateFailedNoRestore);
    }

    [Fact]
    public void AppToStartArgs_ForwardedVerbatimToLaunchedApp()
    {
        var source = _fixture.WriteFile("source.bin", "payload"u8.ToArray());
        var dest = _fixture.PathInWorkDir("dest.bin");

        // The updater base64-decodes --appToStartArgs and prepends them to the args it
        // passes to the appToStart stub, so the .cmd's %* must contain both these tokens
        // AND the --externalUpdaterResult ... that AssertAppLaunchedWith already checks.
        var exit = _fixture.RunUpdaterWithAppArgs(
            "--foo bar --baz qux",
            _fixture.MoveEntry(source, dest));

        Assert.Equal(0, exit);
        Assert.True(_fixture.FileExists(dest));
        _fixture.AssertAppLaunchedWith(
            ExternalUpdaterResult.UpdateSuccess,
            "--foo", "bar", "--baz", "qux");
    }

    [Fact]
    public void Pid_WaitsForParentProcessToExitBeforeApplying()
    {
        var bytes = "payload"u8.ToArray();
        var source = _fixture.WriteFile("source.bin", bytes);
        var dest = _fixture.PathInWorkDir("dest.bin");
        
        // Spawn ping.exe directly (not via cmd /c) so that parent.Kill() terminates the
        // actual long-running process. With cmd.exe as the wrapper, killing cmd leaves
        // ping running as an orphan because Windows doesn't tear down child processes
        // when their parent dies.
        using var parent = Process.Start(new ProcessStartInfo("ping.exe", "-t 127.0.0.1")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true, // swallow the ping output
        });
        Assert.NotNull(parent);

        try
        {
            using var updater = _fixture.StartUpdater(parent.Id, _fixture.MoveEntry(source, dest));

            // Probe the gate directly: while the parent is alive, the updater must not have
            // exited. Without --pid, this same payload completes well under a second; if
            // WaitForExit returns true within this generous window, the wait gate is broken.
            Assert.False(updater.WaitForExit((int)TimeSpan.FromSeconds(2).TotalMilliseconds),
                "Updater exited while its --pid parent was still alive.");
            
            Assert.False(_fixture.FileExists(dest),
                "Updater applied the payload before the --pid parent exited.");

            parent.Kill();
            parent.WaitForExit();

            Assert.True(updater.WaitForExit((int)TimeSpan.FromSeconds(15).TotalMilliseconds),
                "Updater did not exit after the --pid parent was killed.");
            Assert.Equal(0, updater.ExitCode);
        }
        finally
        {
            try
            {
                if (!parent.HasExited)
                    parent.Kill();
            }
            catch
            {
                 // Ignore
            }
        }

        Assert.True(_fixture.FileExists(dest));
        _fixture.AssertAppLaunchedWith(ExternalUpdaterResult.UpdateSuccess);
    }
}
