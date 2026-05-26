using System;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Models;
using AnakinRaW.ExternalUpdater.Options;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test.Wire;

public class UpdaterPayloadFormatTests
{
    private const string ExpectedJson =
        """
        [
            {
                "update": {
                    "file": "src/a.bin",
                    "destination": "dest/a.bin",
                    "integrity": { "hashType": "SHA256", "hash": "abc123" }
                }
            },
            {
                "update": { "file": "stale/b.bin" }
            },
            {
                "update": {
                    "file": "src/c.bin",
                    "destination": "dest/c.bin",
                    "integrity": { "hashType": "SHA512", "hash": "ff00" }
                },
                "backup": {
                    "destination": "dest/c.bin",
                    "source": "bak/c.bak",
                    "integrity": { "hashType": "SHA256", "hash": "deadbeef" }
                }
            },
            {
                "backup": { "destination": "dest/d.bin" }
            }
        ]
        """;

    [Fact]
    public void Payload_SerializesToPinnedFormat()
    {
        var payload = BuildPayload();

        var base64 = payload.ToPayload();
        var actual = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        Assert.Equal(Canonical(ExpectedJson), Canonical(actual));
    }

    [Fact]
    public async Task Payload_RoundTripsThroughPinnedFormat()
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ExpectedJson));
        var options = new ExternalUpdateOptions { AppToStart = "any", Payload = base64 };

        var parsed = (await options.GetUpdateInformationAsync()).ToArray();

        Assert.Equal(4, parsed.Length);

        Assert.Equal("src/a.bin", parsed[0].Update?.File);
        Assert.Equal("dest/a.bin", parsed[0].Update?.Destination);
        Assert.Equal(new IntegrityInformation { HashType = "SHA256", Hash = "abc123" }, parsed[0].Update?.Integrity);

        Assert.Equal("stale/b.bin", parsed[1].Update?.File);
        Assert.Null(parsed[1].Update?.Destination);
        Assert.Null(parsed[1].Update?.Integrity);

        Assert.Equal(new IntegrityInformation { HashType = "SHA512", Hash = "ff00" }, parsed[2].Update?.Integrity);
        Assert.Equal("bak/c.bak", parsed[2].Backup?.Source);
        Assert.Equal(new IntegrityInformation { HashType = "SHA256", Hash = "deadbeef" }, parsed[2].Backup?.Integrity);

        Assert.Null(parsed[3].Update);
        Assert.Equal("dest/d.bin", parsed[3].Backup?.Destination);
        Assert.Null(parsed[3].Backup?.Source);
    }

    private static string Canonical(string json)
    {
        return JsonNode.Parse(json)!.ToJsonString();
    }

    private static UpdateInformation[] BuildPayload() =>
    [
        new()
        {
            Update = new FileCopyInformation
            {
                File = "src/a.bin",
                Destination = "dest/a.bin",
                Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "abc123" },
            },
        },
        new()
        {
            Update = new FileCopyInformation { File = "stale/b.bin" },
        },
        new()
        {
            Update = new FileCopyInformation
            {
                File = "src/c.bin",
                Destination = "dest/c.bin",
                Integrity = new IntegrityInformation { HashType = "SHA512", Hash = "ff00" },
            },
            Backup = new BackupInformation
            {
                Destination = "dest/c.bin",
                Source = "bak/c.bak",
                Integrity = new IntegrityInformation { HashType = "SHA256", Hash = "deadbeef" },
            },
        },
        new()
        {
            Backup = new BackupInformation { Destination = "dest/d.bin" },
        },
    ];
}
