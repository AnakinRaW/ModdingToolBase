using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.ApplicationBase.Options;

public interface IUpdaterCommandLineOptions
{
    public bool AutomaticRestart { get; }

    public bool SkipUpdate { get; }

    public ProductBranch? UpdateBranch { get; }
}