using System.Text.Json;
using AnakinRaW.ExternalUpdater.Models;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test.Models;

public class UpdateInformationSerializationTests
{
    [Fact]
    public void FileCopyInformation_WithAllFields_RoundTrips()
    {
        var original = new FileCopyInformation
        {
            File = @"C:\src\a.bin",
            Destination = @"C:\dest\a.bin",
            Sha256 = "abc123",
        };

        var json = JsonSerializer.Serialize(original);

        Assert.Contains(@"""file"":""C:\\src\\a.bin""", json);
        Assert.Contains(@"""destination"":""C:\\dest\\a.bin""", json);
        Assert.Contains(@"""sha256"":""abc123""", json);

        var parsed = JsonSerializer.Deserialize<FileCopyInformation>(json);

        Assert.NotNull(parsed);
        Assert.Equal(original.File, parsed.File);
        Assert.Equal(original.Destination, parsed.Destination);
        Assert.Equal(original.Sha256, parsed.Sha256);
    }

    [Fact]
    public void FileCopyInformation_DeleteEntry_OmitsNullFields()
    {
        var entry = new FileCopyInformation { File = @"C:\dead.bin" };

        var json = JsonSerializer.Serialize(entry);

        Assert.Contains("\"file\":", json);
        Assert.DoesNotContain("\"destination\":", json);
        Assert.DoesNotContain("\"sha256\":", json);
    }

    [Fact]
    public void BackupInformation_WithSource_RoundTrips()
    {
        var original = new BackupInformation
        {
            Destination = @"C:\install\a.bin",
            Source = @"C:\backup\a.bin",
        };

        var json = JsonSerializer.Serialize(original);
        var parsed = JsonSerializer.Deserialize<BackupInformation>(json);

        Assert.NotNull(parsed);
        Assert.Equal(original.Destination, parsed!.Destination);
        Assert.Equal(original.Source, parsed.Source);
    }

    [Fact]
    public void BackupInformation_WithoutSource_OmitsField()
    {
        var entry = new BackupInformation { Destination = @"C:\install\a.bin" };

        var json = JsonSerializer.Serialize(entry);

        Assert.Contains("\"destination\":", json);
        Assert.DoesNotContain("\"source\":", json);
    }

    [Fact]
    public void UpdateInformation_OnlyUpdate_OmitsBackupField()
    {
        var entry = new UpdateInformation
        {
            Update = new FileCopyInformation { File = "f", Destination = "d", Sha256 = "abc" },
        };

        var json = JsonSerializer.Serialize(entry);

        Assert.Contains("\"update\":", json);
        Assert.DoesNotContain("\"backup\":", json);
    }

    [Fact]
    public void UpdateInformation_OnlyBackup_OmitsUpdateField()
    {
        var entry = new UpdateInformation
        {
            Backup = new BackupInformation { Destination = "d", Source = "s" },
        };

        var json = JsonSerializer.Serialize(entry);

        Assert.DoesNotContain("\"update\":", json);
        Assert.Contains("\"backup\":", json);
    }

    [Fact]
    public void UpdateInformation_BothFields_RoundTrips()
    {
        var original = new UpdateInformation
        {
            Update = new FileCopyInformation { File = "f", Destination = "d", Sha256 = "abc" },
            Backup = new BackupInformation { Destination = "d", Source = "s" },
        };

        var json = JsonSerializer.Serialize(original);
        var parsed = JsonSerializer.Deserialize<UpdateInformation>(json);

        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Update);
        Assert.NotNull(parsed.Backup);
        Assert.Equal("f", parsed.Update!.File);
        Assert.Equal("d", parsed.Update.Destination);
        Assert.Equal("abc", parsed.Update.Sha256);
        Assert.Equal("d", parsed.Backup!.Destination);
        Assert.Equal("s", parsed.Backup.Source);
    }

    [Fact]
    public void UpdateInformation_EmptyObject_DeserializesToEmptyRecord()
    {
        var parsed = JsonSerializer.Deserialize<UpdateInformation>("{}");

        Assert.NotNull(parsed);
        Assert.Null(parsed.Update);
        Assert.Null(parsed.Backup);
    }

    [Fact]
    public void FileCopyInformation_MissingFile_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FileCopyInformation>("{}"));
    }

    [Fact]
    public void BackupInformation_MissingDestination_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BackupInformation>("{}"));
    }
}
