using System;
using AnakinRaW.ApplicationBase.Options;
using Validation;

namespace AnakinRaW.ApplicationBase.Update;

internal sealed class UpdateOptionsProviderService : IUpdateOptionsProviderService
{
    private UpdaterCommandLineOptions Options { get; set; } = null!;

    public UpdaterCommandLineOptions GetOptions()
    {
        if (Options is null)
            throw new InvalidOperationException("Update options are not set.");
        return Options;
    }

    public void SetOptions(UpdaterCommandLineOptions options)
    {
        Requires.NotNull(options, nameof(options));
        Options = options;
    }
}