using CommandLine;

namespace AnakinRaW.ApplicationBase.Update.Options;

// The option names only have long (and verbose) names because it must also be possible to parse them without the 'updateApplication' verb.
// Short names may cause ambiguity with options of the concrete application.
[Verb("updateApplication", isDefault: false, HelpText = "Updates this application.")]
public sealed class ApplicationUpdateOptions
{
    [Option("updateBranch", Default = null, 
        HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? BranchName { get; init; }

    [Option("updateManifestUrl", SetName = "manifestSource", Default = null, Required = false,
        HelpText = "The absolute URL of the update manifest. " +
                   "Mutually exclusive with --updateServerUrl. If neither is specified the default URLs of the application will be used.")]
    public string? ManifestUrl { get; init; }

    [Option("updateServerUrl", SetName = "serverSource", Default = null, Required = false,
        HelpText = "The base URL of a custom update server. The manifest URL is resolved as <updateServerUrl>/<branch>/manifest.json. " +
                   "Mutually exclusive with --updateManifestUrl. If neither is specified the default URLs of the application will be used.")]
    public string? ServerUrl { get; init; }

    [Option("verboseUpdateLogging", Default = false, Required = false,
        HelpText = "Enables verbose logging of the update procedure")]
    public bool Verbose { get; init; }

    [Option("noRestart", Default = false, Required = false,
        HelpText = "Apply the update without relaunching the application after it completes. " +
                   "When the update needs an out-of-process file swap, the external updater " +
                   "applies it and exits; the application is not restarted.")]
    public bool NoRestart { get; init; }
}
