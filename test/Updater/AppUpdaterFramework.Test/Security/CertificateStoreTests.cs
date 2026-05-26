using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Security;

public class CertificateStoreTests : TestBaseWithServiceProvider
{
    private readonly ICertificateStore _store;

    public CertificateStoreTests()
    {
        _store = new CertificateStore(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IFileSystem>(new RealFileSystem());
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }

    [Fact]
    public void Add_ThenContains_ReturnsTrue()
    {
        using var cert = TestCertificates.CreateSelfSigned();
        _store.Add(cert);
        Assert.True(_store.Contains(cert));
    }

    [Fact]
    public void Contains_ReturnsFalse_ForUnknownCert()
    {
        using var added = TestCertificates.CreateSelfSigned();
        using var other = TestCertificates.CreateSelfSigned();
        _store.Add(added);
        Assert.False(_store.Contains(other));
    }

    [Fact]
    public void Add_Idempotent()
    {
        using var cert = TestCertificates.CreateSelfSigned();
        _store.Add(cert);
        _store.Add(cert);
        Assert.True(_store.Contains(cert));
    }

    [Fact]
    public void Remove_PreviouslyAdded_ReturnsTrueAndContainsBecomesFalse()
    {
        using var cert = TestCertificates.CreateSelfSigned();
        _store.Add(cert);
        Assert.True(_store.Remove(cert));
        Assert.False(_store.Contains(cert));
    }

    [Fact]
    public void Remove_NotPresent_ReturnsFalse()
    {
        using var cert = TestCertificates.CreateSelfSigned();
        Assert.False(_store.Remove(cert));
    }

    [Fact]
    public void Contains_WithMultipleAdded_FindsEach()
    {
        using var c1 = TestCertificates.CreateSelfSigned();
        using var c2 = TestCertificates.CreateSelfSigned();
        using var c3 = TestCertificates.CreateSelfSigned();
        _store.Add(c1);
        _store.Add(c2);

        Assert.True(_store.Contains(c1));
        Assert.True(_store.Contains(c2));
        Assert.False(_store.Contains(c3));
    }
}
