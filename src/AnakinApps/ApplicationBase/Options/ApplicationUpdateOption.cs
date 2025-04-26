using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using CommandLine;

namespace AnakinRaW.ApplicationBase.Options;

[Verb("update", HelpText = "Updates this application.")]
public sealed class ApplicationUpdateOption
{
    [Option('b', "branch", Default = null, 
        HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? BranchName { get; init; }

    public ProductBranch? Branch => string.IsNullOrEmpty(BranchName) ? null : new ProductBranch(BranchName!, false);
}