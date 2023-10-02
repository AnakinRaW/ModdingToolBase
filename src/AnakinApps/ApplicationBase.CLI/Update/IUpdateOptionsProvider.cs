using AnakinRaW.ApplicationBase.Options;

namespace AnakinRaW.ApplicationBase.Update;

internal interface IUpdateOptionsProvider
{
    IUpdaterCommandLineOptions GetOptions();
}