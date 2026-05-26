using Xunit;

namespace AnakinRaW.ExternalUpdater.Core.Test;

public class ExternalUpdaterResultOptionsTests
{
    [Fact]
    public void TryParse_EmptyArgs_ReturnsDefault()
    {
        var ok = ExternalUpdaterResultOptions.TryParse([], out var options);

        Assert.True(ok);
        Assert.NotNull(options);
        Assert.Equal(ExternalUpdaterResult.UpdaterNotRun, options.Result);
    }

    [Theory]
    [InlineData(ExternalUpdaterResult.UpdateSuccess)]
    [InlineData(ExternalUpdaterResult.UpdateFailedNoRestore)]
    [InlineData(ExternalUpdaterResult.UpdateFailedWithRestore)]
    [InlineData(ExternalUpdaterResult.Restarted)]
    [InlineData(ExternalUpdaterResult.UpdaterNotRun)]
    public void TryParse_KnownResult_ParsesIt(ExternalUpdaterResult expected)
    {
        var ok = ExternalUpdaterResultOptions.TryParse(
            [ExternalUpdaterResultOptions.RawOptionString, expected.ToString()],
            out var options);

        Assert.True(ok);
        Assert.NotNull(options);
        Assert.Equal(expected, options.Result);
    }

    [Fact]
    public void TryParse_UnknownArgsBeforeAndAfter_StillExtractsResult()
    {
        // Main app may forward arbitrary command-line arguments through. TryParse must
        // ignore them and still pick up --externalUpdaterResult.
        var args = new[]
        {
            "--some-app-arg", "value",
            ExternalUpdaterResultOptions.RawOptionString, nameof(ExternalUpdaterResult.UpdateSuccess),
            "extraPositional",
        };

        var ok = ExternalUpdaterResultOptions.TryParse(args, out var options);

        Assert.True(ok);
        Assert.Equal(ExternalUpdaterResult.UpdateSuccess, options!.Result);
    }

    [Fact]
    public void TryParse_OnlyUnknownArgs_ReturnsDefault()
    {
        var ok = ExternalUpdaterResultOptions.TryParse(
            ["--foo", "bar", "baz"],
            out var options);

        Assert.True(ok);
        Assert.Equal(ExternalUpdaterResult.UpdaterNotRun, options!.Result);
    }

    [Fact]
    public void RawOptionString_MatchesLongName()
    {
        Assert.Equal($"--{ExternalUpdaterResultOptions.OptionLongName}", ExternalUpdaterResultOptions.RawOptionString);
    }
}
