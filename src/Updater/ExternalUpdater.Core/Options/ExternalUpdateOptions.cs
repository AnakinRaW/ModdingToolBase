using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Models;
using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

/// <summary>Represents the command-line options for the external updater's <c>update</c> verb, which applies a batch of file operations and then restarts the host application.</summary>
[Verb("update", HelpText = "Updates the given application and restarts it")]
public sealed record ExternalUpdateOptions : ExternalUpdaterOptions
{
    private IReadOnlyCollection<UpdateInformation>? _updateItems;

    /// <summary>Gets the Base64-encoded JSON payload that describes the update operations to perform.</summary>
    /// <value>The Base64-encoded payload as passed on the command line.</value>
    [Option("updatePayload", Required = true, HelpText = "Base64-encoded JSON payload describing the update operations.")]
    public required string Payload { get; init; }

    /// <summary>Asynchronously decodes <see cref="Payload"/> into the collection of update operations it describes.</summary>
    /// <returns>A task that produces the decoded collection of update operations.</returns>
    public async Task<IReadOnlyCollection<UpdateInformation>> GetUpdateInformationAsync()
    {
        if (_updateItems is not null)
            return _updateItems;
        return _updateItems = await this.DecodeAndParsePayload();
    }
}
