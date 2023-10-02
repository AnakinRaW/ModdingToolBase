using CommandLine;
#if NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace AnakinRaW.ExternalUpdater;

public enum ExternalUpdaterResult
{
    UpdateFailedNoRestore = -2,
    UpdateFailedWithRestore = -1,
    UpdaterNotRun = 0,
    UpdateSuccess = 1,
    Restarted = 2
}

public class ExternalUpdaterResultOptions
{
    [Option("externalUpdaterResult", Default = ExternalUpdaterResult.UpdaterNotRun, Required = false)]
    public ExternalUpdaterResult Result { get; init; }

    public static bool TryParse(
        string[] args,
#if NETSTANDARD2_1_OR_GREATER
        [NotNullWhen(true)]
#endif
        out ExternalUpdaterResultOptions? options)
    {
        var parser = new Parser(settings =>
        {
            settings.IgnoreUnknownArguments = true;
        });
        options = parser.ParseArguments<ExternalUpdaterResultOptions>(args).Value;
        return options is not null;
    }
}