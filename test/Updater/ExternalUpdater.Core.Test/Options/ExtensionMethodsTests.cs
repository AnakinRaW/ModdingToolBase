using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using AnakinRaW.ExternalUpdater.Models;
using AnakinRaW.ExternalUpdater.Options;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test.Options;

public class ExtensionMethodsTests
{
    [Fact]
    public void Serialize_EmptyCollection_ProducesEmptyJsonArray()
    {
        var json = ((IEnumerable<UpdateInformation>)[]).Serialize();
        Assert.Equal("[]", json);
    }

    [Fact]
    public void Serialize_SingleEntry_RoundTrips()
    {
        var items = new[]
        {
            new UpdateInformation
            { 
                Update = new FileCopyInformation { File = "a", Destination = "d", Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h" } }
            }
        };

        var json = items.Serialize();
        var parsed = JsonSerializer.Deserialize<IReadOnlyCollection<UpdateInformation>>(json);

        Assert.NotNull(parsed);
        var item = Assert.Single(parsed!);
        Assert.Equal("a", item.Update?.File);
        Assert.Equal("d", item.Update?.Destination);
        Assert.Equal(new IntegrityInformation { HashType = "SHA256", Hash = "h" }, item.Update?.Integrity);
    }

    [Fact]
    public void ToPayload_EmptyCollection_ReturnsBase64EmptyArray()
    {
        var payload = ((IEnumerable<UpdateInformation>)[]).ToPayload();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        Assert.Equal("[]", decoded);
    }

    [Fact]
    public void ToPayload_RoundTrip_ProducesSameItems()
    {
        var items = new[]
        {
            new UpdateInformation { Update = new FileCopyInformation { File = "a", Destination = "ad", Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "h1" } } },
            new UpdateInformation { Update = new FileCopyInformation { File = "b" } },
            new UpdateInformation { Backup = new BackupInformation { Destination = "x", Source = "xs" } },
        };

        var payload = items.ToPayload();
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var parsed = JsonSerializer.Deserialize<IReadOnlyCollection<UpdateInformation>>(decoded);

        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Count);
    }

    [Fact]
    public void ToPayload_IsValidBase64()
    {
        var items = new[] { new UpdateInformation { Update = new FileCopyInformation { File = "f" } } };
        var payload = items.ToPayload();

        // Should not throw — payload must be valid base64.
        var bytes = Convert.FromBase64String(payload);
        Assert.NotEmpty(bytes);
    }
}
