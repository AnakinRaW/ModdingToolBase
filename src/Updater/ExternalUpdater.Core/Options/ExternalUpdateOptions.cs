using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Models;
using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

[Verb("update", HelpText = "Updates the given application and restarts it")]
public sealed record ExternalUpdateOptions : ExternalUpdaterOptions
{
    private IReadOnlyCollection<UpdateInformation>? _updateItems;

    [Option("updatePayload", Required = true, HelpText = "Base64-encoded JSON payload describing the update operations.")]
    public required string Payload { get; init; }

    public async Task<IReadOnlyCollection<UpdateInformation>> GetUpdateInformationAsync()
    {
        if (_updateItems is not null)
            return _updateItems;
        return _updateItems = await this.DecodeAndParsePayload();
    }
}
