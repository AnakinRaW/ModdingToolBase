using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace AnakinRaW.ExternalUpdater;

public class ExternalUpdaterResultOptions
{
    public const string RawOptionString = $"--{OptionLongName}";

    public const string OptionLongName = "externalUpdaterResult";

    [Option(OptionLongName, Default = ExternalUpdaterResult.UpdaterNotRun, Required = false)]
    public ExternalUpdaterResult Result { get; init; }

    public static bool TryParse(string[] args, [NotNullWhen(true)] out ExternalUpdaterResultOptions? options)
    {
        var parser = new Parser(settings =>
        {
            // The external updater may pass through other arguments from the application to be restarted.
            settings.IgnoreUnknownArguments = true;
        });
        options = parser.ParseArguments<ExternalUpdaterResultOptions>(args).Value;
        return options is not null;
    }
}