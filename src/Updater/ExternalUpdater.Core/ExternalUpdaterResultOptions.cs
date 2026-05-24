using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace AnakinRaW.ExternalUpdater;

/// <summary>Represents the command-line option used to forward the <see cref="ExternalUpdaterResult"/> from the external updater to the restarted application.</summary>
public class ExternalUpdaterResultOptions
{
    /// <summary>The raw command-line switch, including the leading double dash, that carries the external updater result.</summary>
    public const string RawOptionString = $"--{OptionLongName}";

    /// <summary>The long name of the command-line option that carries the external updater result.</summary>
    public const string OptionLongName = "externalUpdaterResult";

    /// <summary>Gets the result reported by the external updater.</summary>
    /// <value>One of the enumeration values that specifies the outcome of the updater run. The default is <see cref="ExternalUpdaterResult.UpdaterNotRun"/>.</value>
    [Option(OptionLongName, Default = ExternalUpdaterResult.UpdaterNotRun, Required = false)]
    public ExternalUpdaterResult Result { get; init; }

    /// <summary>Attempts to parse an <see cref="ExternalUpdaterResultOptions"/> instance from the given command-line arguments.</summary>
    /// <remarks>
    /// Unknown arguments in <paramref name="args"/> are ignored so that pass-through arguments from the restarted application do not cause parsing to fail.
    /// </remarks>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="options">When this method returns, contains the parsed options if parsing succeeded, or <see langword="null"/> if it failed. This parameter is treated as uninitialized.</param>
    /// <returns><see langword="true"/> if <paramref name="args"/> was parsed successfully; otherwise, <see langword="false"/>.</returns>
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