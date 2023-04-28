﻿using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using CommandLine;

namespace AnakinRaW.ApplicationBase;

public abstract class UpdaterCommandLineOptions
{
    [Option("automaticRestart", Default = false, HelpText = "When true, the the application automatically restarts if necessary for the update.")]
    public bool AutomaticRestart { get; init; }

    [Option("updateBranch", Default = null, HelpText = "The branch that shall be used for updating. If no branch is specified the current branch will be taken")]
    public string? UpdateBranchName { get; init; }

    public ProductBranch? UpdateBranch => string.IsNullOrEmpty(UpdateBranchName) ? null :  new ProductBranch(UpdateBranchName!, false);
}

[Verb("update")]
public class UpdateOptions : UpdaterCommandLineOptions
{
}