using System;
using AnakinRaW.ApplicationBase.Options;

namespace AnakinRaW.ApplicationBase.Update;

internal sealed class UpdateOptionsProviderService : IUpdateOptionsProviderService
{
    private IUpdaterCommandLineOptions Options { get; set; } = null!;

    public IUpdaterCommandLineOptions GetOptions()
    {
        if (Options is null)
            throw new InvalidOperationException("Update options are not set.");
        return Options;
    }

    public void SetOptions(IUpdaterCommandLineOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }
}