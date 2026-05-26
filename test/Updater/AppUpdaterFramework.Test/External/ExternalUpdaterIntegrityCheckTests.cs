using System;
using System.IO;
using System.Security;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.External;

public class ExternalUpdaterIntegrityCheckTests : TestBaseWithFileSystem, IDisposable
{
    private readonly IHashingService _hashingService;
    private readonly IExternalUpdaterIntegrityCheck _check;
    private readonly string _tempFile;

    public ExternalUpdaterIntegrityCheckTests()
    {
        _hashingService = ServiceProvider.GetRequiredService<IHashingService>();
        _check = new ExternalUpdaterIntegrityCheck(ServiceProvider);
        var tempDir = FileSystem.Path.GetTempPath();
        FileSystem.Directory.CreateDirectory(tempDir);
        _tempFile = FileSystem.Path.Combine(tempDir, $"updater-integrity-check-{Guid.NewGuid():N}.bin");
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    public void Dispose()
    {
        if (FileSystem.File.Exists(_tempFile))
            FileSystem.File.Delete(_tempFile);
    }

    private Stream OpenForCheck()
    {
        return FileSystem.File.Open(_tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    [Fact]
    public void EnsureMatchesAny_HashMatchesSingleAcceptable_DoesNotThrow()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        FileSystem.File.WriteAllBytes(_tempFile, bytes);
        var file = FileSystem.FileInfo.New(_tempFile);
        var expected = _hashingService.GetHash(file, HashTypeKey.SHA256);

        using var s = OpenForCheck();
        _check.EnsureMatchesAny(s, [new ComponentIntegrityInformation(expected, HashTypeKey.SHA256)]);
    }

    [Fact]
    public void EnsureMatchesAny_HashMatchesAnyOfMultiple_DoesNotThrow()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        FileSystem.File.WriteAllBytes(_tempFile, bytes);
        var file = FileSystem.FileInfo.New(_tempFile);
        var actual = _hashingService.GetHash(file, HashTypeKey.SHA256);

        using var s = OpenForCheck();
        _check.EnsureMatchesAny(
            s,
            [
                new ComponentIntegrityInformation(new byte[32], HashTypeKey.SHA256),
                new ComponentIntegrityInformation(actual, HashTypeKey.SHA256)
            ]);
    }

    [Fact]
    public void EnsureMatchesAny_NoEnforceableMatchButWaiverPresent_AcceptsWithWarning()
    {
        FileSystem.File.WriteAllBytes(_tempFile, [0x10, 0x20, 0x30]);

        using var s = OpenForCheck();
        _check.EnsureMatchesAny(
            s,
            [
                new ComponentIntegrityInformation(new byte[32], HashTypeKey.SHA256),    // enforceable, won't match
                ComponentIntegrityInformation.None,                                         // waiver
            ]);
    }

    [Fact]
    public void EnsureMatchesAny_HashMatchesNone_ThrowsSecurityException()
    {
        FileSystem.File.WriteAllBytes(_tempFile, [1, 2, 3, 4, 5]);

        using var s = OpenForCheck();
        Assert.Throws<SecurityException>(() => _check.EnsureMatchesAny(
            s,
            [
                new ComponentIntegrityInformation(new byte[32], HashTypeKey.SHA256),
                new ComponentIntegrityInformation(new byte[32], HashTypeKey.SHA256),
            ]));
    }

    [Fact]
    public void EnsureMatchesAny_EmptyAcceptable_IsNoOp()
    {
        FileSystem.File.WriteAllBytes(_tempFile, [0xAA]);

        using var s = OpenForCheck();
        _check.EnsureMatchesAny(s, []);
    }

    [Fact]
    public void EnsureMatchesAny_AllAcceptablesAreNoIntegrity_IsNoOp()
    {
        FileSystem.File.WriteAllBytes(_tempFile, [0xAA]);

        using var s = OpenForCheck();
        _check.EnsureMatchesAny(
            s,
            [ComponentIntegrityInformation.None, new ComponentIntegrityInformation(null, HashTypeKey.SHA256)]);
    }

    [Fact]
    public void EnsureMatchesAny_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _check.EnsureMatchesAny(null!, []));
    }

    [Fact]
    public void EnsureMatchesAny_NullAcceptable_Throws()
    {
        FileSystem.File.WriteAllBytes(_tempFile, [0xAA]);
        using var s = OpenForCheck();
        Assert.Throws<ArgumentNullException>(() => _check.EnsureMatchesAny(s, null!));
    }

    [Fact]
    public void Ctor_NullServiceProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ExternalUpdaterIntegrityCheck(null!));
    }
}
