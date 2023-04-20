using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using CommandLine;

namespace AnakinRaW.ApplicationBase;

internal class CommandLineUpdaterOptions
{
    public static readonly CommandLineUpdaterOptions Default = new();

    [Option("skip", Default = false, HelpText = "Skips the whole update procedure.")]
    public bool Skip { get; init; }

    [Option("branch", Default = null, HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? BranchName { get; init; }

    public ProductBranch? Branch => string.IsNullOrEmpty(BranchName) ? null :  new ProductBranch(BranchName!, false);
}