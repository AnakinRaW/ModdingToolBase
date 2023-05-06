using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using CommandLine;

namespace AnakinRaW.ApplicationBase.Options;

public class UpdaterCommandLineOptions : IUpdaterCommandLineOptions
{
    [Option("automaticRestart",
        HelpText = "When true, the the application automatically restarts if necessary for the update.")]
    public bool AutomaticRestart { get; init; } = true; // We cannot use the Attribute for setting the default!

    [Option("updateBranch", Default = null, HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? UpdateBranchName { get; init; }

    [Option("skipUpdate")]
    public bool SkipUpdate { get; init; }

    public ProductBranch? UpdateBranch => string.IsNullOrEmpty(UpdateBranchName) ? null : new ProductBranch(UpdateBranchName!, false);
}