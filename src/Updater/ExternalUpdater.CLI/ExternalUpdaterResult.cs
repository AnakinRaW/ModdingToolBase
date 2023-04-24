using System;
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


    public static string[] RemoveFromCurrentArgs(string[] currentArgs)
    {
        if (!TryParse(currentArgs, out var parsed))
            throw new InvalidOperationException("Cannot remove non-existing args.");

        var argsToRemove = Parser.Default.FormatCommandLineArgs(parsed);
        var numToRemove = argsToRemove.Length;

        var newArgs = new string[currentArgs.Length - numToRemove];
        var splitIndex = Array.IndexOf(currentArgs, argsToRemove[0]); 
        Array.Copy(currentArgs, 0, newArgs, 0, splitIndex);
        Array.Copy(currentArgs, splitIndex + numToRemove, newArgs, splitIndex, currentArgs.Length - splitIndex - numToRemove);

        return newArgs;
    }
}