using CommandLine;

namespace AnakinRaW.ApplicationBase.Options;

[Verb("update", HelpText = "Updates this application.")]
public sealed class UpdateOption : UpdaterCommandLineOptions
{
}