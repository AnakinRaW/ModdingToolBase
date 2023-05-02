using AnakinRaW.ApplicationBase.Options;

namespace AnakinRaW.ApplicationBase.Update;

internal interface IUpdateOptionsProvider
{
    UpdaterCommandLineOptions GetOptions();
}