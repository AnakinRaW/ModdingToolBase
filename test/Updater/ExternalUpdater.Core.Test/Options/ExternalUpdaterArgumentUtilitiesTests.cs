using AnakinRaW.ExternalUpdater.Options;
using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test.Options;

public class ExternalUpdaterArgumentUtilitiesTests
{
    [Fact]
    public void ToArgs_UpdateOptions_IncludesVerbAndRequiredOptions()
    {
        var options = new ExternalUpdateOptions
        {
            AppToStart = @"C:\app\main.exe",
            Payload = "WyJpdGVtIl0=" // ["item"]
        };

        var args = options.ToArgs();

        Assert.Contains("update", args);
        Assert.Contains("--appToStart", args);
        Assert.Contains(@"C:\app\main.exe", args);
        Assert.Contains("--updatePayload", args);
        Assert.Contains("WyJpdGVtIl0=", args);
    }

    [Fact]
    public void ToArgs_RestartOptions_IncludesRestartVerb()
    {
        var options = new ExternalRestartOptions
        {
            AppToStart = @"C:\app\main.exe",
        };

        var args = options.ToArgs();

        Assert.Contains("restart", args);
        Assert.Contains("--appToStart", args);

        Assert.DoesNotContain("--elevate", args);
        Assert.DoesNotContain("--pid", args);
        Assert.DoesNotContain("--appToStartArgs", args);
    }

    [Fact]
    public void ToArgs_PidProvided_IncludesIt()
    {
        var options = new ExternalRestartOptions
        {
            AppToStart = "x",
            Pid = 1234,
        };

        var args = options.ToArgs();

        Assert.Contains("--pid 1234", args);
    }

    [Fact]
    public void ToArgs_ElevateTrue_IncludesFlag()
    {
        var options = new ExternalRestartOptions
        {
            AppToStart = "x",
            Elevate = true,
        };

        var args = options.ToArgs();

        Assert.Contains("--elevate", args);
    }

    [Fact]
    public void ToArgs_AppToStartArgumentsProvided_Emitted()
    {
        var options = new ExternalRestartOptions
        {
            AppToStart = "x",
            AppToStartArguments = "Zm9vIGJhcg==",   // base64('foo bar')
        };

        var args = options.ToArgs();

        Assert.Contains("--appToStartArgs", args);
        Assert.Contains("Zm9vIGJhcg==", args);
    }
}
