using CommandLine;

namespace AnakinRaW.ApplicationManifestSigner;

internal sealed class SignerOptions
{
    [Option('m', "manifest", Required = true, HelpText = "Path to the manifest file to sign.")]
    public string ManifestPath { get; init; } = string.Empty;

    [Option("pfx", Required = true, HelpText = "Path to the PFX file containing the signing key.")]
    public string PfxPath { get; init; } = string.Empty;

    [Option("password", Required = false, HelpText = "Password for the PFX file. Omit if the PFX is unprotected.")]
    public string? PfxPassword { get; init; }

    [Option('o', "output", Required = false, HelpText = "Output path for the signed manifest. Defaults to overwriting the input file.")]
    public string? OutputPath { get; init; }
}
