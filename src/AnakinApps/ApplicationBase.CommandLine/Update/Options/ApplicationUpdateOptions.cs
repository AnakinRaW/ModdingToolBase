using CommandLine;

namespace AnakinRaW.ApplicationBase.Update.Options;

// The option names only have long (and verbose) names because it must also be possible to parse them without the 'update' verb.
// Short names may cause ambiguity with options of the concrete application.
[Verb("update", isDefault: false, HelpText = "Updates this application.")]
public sealed class ApplicationUpdateOptions
{
    [Option("updateBranch", Default = null, 
        HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? BranchName { get; init; }

    [Option("updateManifestUrl", Default = null, Required = false, 
        HelpText = "The base URL where to pull update information from. If no URL is specified the default URLs of the application will be used.")]
    public string? ManifestUrl { get; init; }
}
