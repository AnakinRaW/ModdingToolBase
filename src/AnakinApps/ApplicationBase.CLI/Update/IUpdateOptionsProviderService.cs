using AnakinRaW.ApplicationBase.Options;

namespace AnakinRaW.ApplicationBase.Update;

internal interface IUpdateOptionsProviderService : IUpdateOptionsProvider
{
    void SetOptions(IUpdaterCommandLineOptions options);
}