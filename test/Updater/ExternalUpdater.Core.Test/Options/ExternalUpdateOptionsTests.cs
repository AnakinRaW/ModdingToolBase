using System;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Models;
using AnakinRaW.ExternalUpdater.Options;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test.Options;

public class ExternalUpdateOptionsTests
{
    [Fact]
    public async Task GetUpdateInformationAsync_EmptyPayload_ReturnsEmpty()
    {
        var options = new ExternalUpdateOptions
        {
            AppToStart = "anything",
            Payload = ""
        };

        var items = await options.GetUpdateInformationAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetUpdateInformationAsync_EmptyJsonArray_ReturnsEmpty()
    {
        var options = OptionsWith(Encode("[]"));

        var items = await options.GetUpdateInformationAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetUpdateInformationAsync_SingleMoveEntry_DecodesRoundTrip()
    {
        var source = new[]
        {
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = @"C:\src\a.bin",
                    Destination = @"C:\dest\a.bin",
                    Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "abc" },
                },
            },
        };
        var options = OptionsWith(source.ToPayload());

        var items = await options.GetUpdateInformationAsync();

        var item = Assert.Single(items);
        Assert.NotNull(item.Update);
        Assert.Equal(@"C:\src\a.bin", item.Update!.File);
        Assert.Equal(@"C:\dest\a.bin", item.Update.Destination);
        Assert.Equal(new IntegrityInformation { HashType = "SHA256", Hash = "abc" }, item.Update.Integrity);
        Assert.Null(item.Backup);
    }

    [Fact]
    public async Task GetUpdateInformationAsync_MultipleEntries_DecodesInOrder()
    {
        var source = new[]
        {
            new UpdateInformation { Update = new FileCopyInformation { File = "a", Destination = "ad", Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h1" } } },
            new UpdateInformation { Update = new FileCopyInformation { File = "b", Destination = "bd", Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h2" } } },
            new UpdateInformation { Backup = new BackupInformation { Destination = "x", Source = "xs" } },
        };
        var options = OptionsWith(source.ToPayload());

        var items = await options.GetUpdateInformationAsync();

        Assert.Collection(items,
            i => Assert.Equal("a", i.Update?.File),
            i => Assert.Equal("b", i.Update?.File),
            i => Assert.Equal("x", i.Backup?.Destination));
    }

    [Fact]
    public async Task GetUpdateInformationAsync_MixedHashTypes_DecodedPerEntry()
    {
        var source = new[]
        {
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = "a", Destination = "ad",
                    Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h256" },
                },
            },
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = "b", Destination = "bd",
                    Integrity = new IntegrityInformation { HashType = "SHA512", Hash = "h512" },
                },
            },
            new UpdateInformation
            {
                Update = new FileCopyInformation
                {
                    File = "c", Destination = "cd",
                    Integrity = new IntegrityInformation { HashType = "MD5", Hash = "hmd5" },
                },
            },
        };
        var options = OptionsWith(source.ToPayload());

        var items = await options.GetUpdateInformationAsync();

        Assert.Collection(items,
            i => Assert.Equal(new IntegrityInformation { HashType = "SHA256", Hash = "h256" }, i.Update?.Integrity),
            i => Assert.Equal(new IntegrityInformation { HashType = "SHA512", Hash = "h512" }, i.Update?.Integrity),
            i => Assert.Equal(new IntegrityInformation { HashType = "MD5",    Hash = "hmd5" }, i.Update?.Integrity));
    }

    [Fact]
    public async Task GetUpdateInformationAsync_DeleteEntry_DecodesWithNulls()
    {
        var source = new[]
        {
            new UpdateInformation
            {
                Update = new FileCopyInformation { File = @"C:\dead.bin" },
            },
        };
        var options = OptionsWith(source.ToPayload());

        var items = await options.GetUpdateInformationAsync();

        var item = Assert.Single(items);
        Assert.NotNull(item.Update);
        Assert.Equal(@"C:\dead.bin", item.Update!.File);
        Assert.Null(item.Update.Destination);
        Assert.Null(item.Update.Integrity);
    }

    [Fact]
    public async Task GetUpdateInformationAsync_CalledTwice_CachesResult()
    {
        var source = new[]
        {
            new UpdateInformation { Update = new FileCopyInformation { File = "a", Destination = "d", Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h" } } },
        };
        var options = OptionsWith(source.ToPayload());

        var first = await options.GetUpdateInformationAsync();
        var second = await options.GetUpdateInformationAsync();

        // Same instance — cached, not re-decoded.
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetUpdateInformationAsync_InvalidBase64_Throws()
    {
        var options = OptionsWith("not-valid-base64-!!!");

        await Assert.ThrowsAsync<FormatException>(async () => await options.GetUpdateInformationAsync());
    }

    private static ExternalUpdateOptions OptionsWith(string payload)
    {
        return new ExternalUpdateOptions { AppToStart = "any", Payload = payload };
    }

    private static string Encode(string json)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}
