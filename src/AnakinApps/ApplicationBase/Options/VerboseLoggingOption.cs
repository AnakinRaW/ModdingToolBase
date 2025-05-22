using CommandLine;

namespace AnakinRaW.ApplicationBase.Options;

// The option name has a long (and verbose) names because it must also be possible to parse it along other options.
// Short names may cause ambiguity with options of the concrete application.
public sealed class VerboseLoggingOption
{
    [Option("verboseBootstrapLogging", Required = false, HelpText = "Enable verbose logging for the bootstrapping process of the application.")]
    public bool VerboseBootstrapLogging { get; set; } = false;
}