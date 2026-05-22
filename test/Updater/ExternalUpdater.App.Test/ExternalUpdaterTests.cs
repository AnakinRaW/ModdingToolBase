using System;
using System.Text;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using AnakinRaW.ExternalUpdater.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Test;

public class ExternalUpdaterTests : TestBaseWithFileSystem
{
    private readonly IHashingService _hashing;
    private readonly string _workDir;

    public ExternalUpdaterTests()
    {
        _hashing = ServiceProvider.GetRequiredService<IHashingService>();
        _workDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        FileSystem.Directory.CreateDirectory(_workDir);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    [Fact]
    public void Run_SimpleMoveWithValidHash_MovesSourceToDestination()
    {
        var bytes = "hello world"u8.ToArray();
        var sourcePath = WriteFile("a.bin", bytes);
        var destPath = FileSystem.Path.Combine(_workDir, "a-dest.bin");

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = sourcePath, Destination = destPath, Integrity = Sha256Of(sourcePath) }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.False(FileSystem.File.Exists(sourcePath));
        Assert.True(FileSystem.File.Exists(destPath));
        Assert.Equal(bytes, FileSystem.File.ReadAllBytes(destPath));
    }

    [Fact]
    public void Run_DeleteAction_RemovesFileAndDoesNotRequireHash()
    {
        var target = WriteFile("dead.bin", [1, 2, 3]);

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = target, Destination = null, Integrity = null }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.False(FileSystem.File.Exists(target));
    }

    [Fact]
    public void Run_IntegrityMismatch_AbortsBatchWithoutMoving()
    {
        var aBytes = "AAA"u8.ToArray();
        var bBytes = "BBB"u8.ToArray();
        var aSource = WriteFile("a.bin", aBytes);
        var bSource = WriteFile("b.bin", bBytes);
        var aDest = FileSystem.Path.Combine(_workDir, "a-dest.bin");
        var bDest = FileSystem.Path.Combine(_workDir, "b-dest.bin");
        var aWrong = new IntegrityInformation { HashType = HashTypeKey.SHA256.Name, Hash = new string('0', 64) };

        var result = Run(
            new UpdateInformation { Update = new FileCopyInformation { File = aSource, Destination = aDest, Integrity = aWrong } },
            new UpdateInformation { Update = new FileCopyInformation { File = bSource, Destination = bDest, Integrity = Sha256Of(bSource) } });

        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, result);
        Assert.False(FileSystem.File.Exists(aDest));
        Assert.False(FileSystem.File.Exists(bDest));
    }

    [Fact]
    public void Run_MissingIntegrityOnMoveEntry_AbortsBatch()
    {
        var source = WriteFile("x.bin", [9, 9, 9]);
        var dest = FileSystem.Path.Combine(_workDir, "x-dest.bin");

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = source, Destination = dest, Integrity = null }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, result);
        Assert.False(FileSystem.File.Exists(dest));
    }

    [Fact]
    public void Run_SourceFileMissing_AbortsBatch()
    {
        var ghost = FileSystem.Path.Combine(_workDir, "ghost.bin");
        var dest = FileSystem.Path.Combine(_workDir, "ghost-dest.bin");

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation
            {
                File = ghost,
                Destination = dest,
                Integrity = new IntegrityInformation { HashType = HashTypeKey.SHA256.Name, Hash = new string('a', 64) },
            }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, result);
        Assert.False(FileSystem.File.Exists(dest));
    }

    [Fact]
    public void Run_HexCaseInsensitive_StillMatches()
    {
        var bytes = "payload"u8.ToArray();
        var source = WriteFile("u.bin", bytes);
        var dest = FileSystem.Path.Combine(_workDir, "u-dest.bin");
        var declared = Sha256Of(source) with { Hash = Sha256Of(source).Hash.ToUpperInvariant() };

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = source, Destination = dest, Integrity = declared }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.True(FileSystem.File.Exists(dest));
    }

    [Fact]
    public void Run_Sha512Algorithm_MovesWhenHashMatches()
    {
        var bytes = "payload-512"u8.ToArray();
        var source = WriteFile("s.bin", bytes);
        var dest = FileSystem.Path.Combine(_workDir, "s-dest.bin");
        var integrity = new IntegrityInformation
        {
            HashType = HashTypeKey.SHA512.Name,
            Hash = Hex(_hashing.GetHash(FileSystem.FileInfo.New(source), HashTypeKey.SHA512)),
        };

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = source, Destination = dest, Integrity = integrity }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.True(FileSystem.File.Exists(dest));
    }

    [Fact]
    public void Run_UnsupportedHashType_AbortsBatch()
    {
        var source = WriteFile("x.bin", [1, 2, 3]);
        var dest = FileSystem.Path.Combine(_workDir, "x-dest.bin");
        var integrity = new IntegrityInformation { HashType = "BOGUS", Hash = new string('a', 64) };

        var result = Run(new UpdateInformation
        {
            Update = new FileCopyInformation { File = source, Destination = dest, Integrity = integrity }
        });

        Assert.Equal(ExternalUpdaterResult.UpdateFailedNoRestore, result);
        Assert.False(FileSystem.File.Exists(dest));
    }

    [Fact]
    public void Run_MultipleValidItems_AllApplied()
    {
        var (s1, d1, i1) = MakeMoveTarget("one.bin", [0x10]);
        var (s2, d2, i2) = MakeMoveTarget("two.bin", [0x20, 0x21]);
        var (s3, d3, i3) = MakeMoveTarget("three.bin", [0x30, 0x31, 0x32]);

        var result = Run(
            new UpdateInformation { Update = new FileCopyInformation { File = s1, Destination = d1, Integrity = i1 } },
            new UpdateInformation { Update = new FileCopyInformation { File = s2, Destination = d2, Integrity = i2 } },
            new UpdateInformation { Update = new FileCopyInformation { File = s3, Destination = d3, Integrity = i3 } });

        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, result);
        Assert.True(FileSystem.File.Exists(d1));
        Assert.True(FileSystem.File.Exists(d2));
        Assert.True(FileSystem.File.Exists(d3));
    }

    [Fact]
    public void Run_EmptyPayload_Success()
    {
        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, Run());
    }

    private ExternalUpdaterResult Run(params UpdateInformation[] items)
    {
        var updater = new Utilities.ExternalUpdater(items, ServiceProvider);
        return updater.Run();
    }

    private string WriteFile(string name, byte[] bytes)
    {
        var path = FileSystem.Path.Combine(_workDir, name);
        var parent = FileSystem.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
            FileSystem.Directory.CreateDirectory(parent!);
        FileSystem.File.WriteAllBytes(path, bytes);
        return path;
    }

    private (string source, string dest, IntegrityInformation integrity) MakeMoveTarget(string name, byte[] bytes)
    {
        var source = WriteFile(name, bytes);
        var dest = FileSystem.Path.Combine(_workDir, name + ".dest");
        return (source, dest, Sha256Of(source));
    }

    private IntegrityInformation Sha256Of(string path) =>
        new()
        {
            HashType = HashTypeKey.SHA256.Name,
            Hash = Hex(_hashing.GetHash(FileSystem.FileInfo.New(path), HashTypeKey.SHA256)),
        };

    private static string Hex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
