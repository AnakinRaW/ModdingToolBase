using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

/// <summary>Represents the command-line options for the external updater's <c>restart</c> verb, which waits for a given process to exit and then launches another one.</summary>
[Verb("restart",
    HelpText = "Waits for a given process to close and starts a new given process (usually the one that was just closed).")]
public sealed record ExternalRestartOptions : ExternalUpdaterOptions;
