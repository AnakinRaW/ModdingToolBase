using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using CommandLine;

namespace AnakinRaW.ApplicationBase;

public class UpdaterCommandLineOptions
{
    public static readonly UpdaterCommandLineOptions Default = new();

    [Option("skipUpdate", Default = false, HelpText = "Skips the whole update procedure.")]
    public bool SkipUpdate { get; init; }

    [Option("updateBranch", Default = null, HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? UpdateBranchName { get; init; }

    public ProductBranch? UpdateBranch => string.IsNullOrEmpty(UpdateBranchName) ? null :  new ProductBranch(UpdateBranchName!, false);
}