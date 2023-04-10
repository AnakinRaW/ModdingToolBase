using System;
using System.Collections.Generic;
using CommandLine;

namespace AnakinRaW.ApplicationManifestCreator;

internal class ManifestCreatorOptions
{
    [Option('a', "applicationFile", Required = true, HelpText = "The file of the application to create the manifest for.")]
    public string ApplicationFile { get; init; }

    [Option('o', "output", Required = true, HelpText = "File path of the manifest file.")]
    public string OuputPath { get; init; }

    [Option("origin", Required = true, HelpText = "Root url where all components are expected to be deployed. Must be an absolute uri.")]
    public string Origin { get; init; }

    [Option("appDataFiles", HelpText = "Files which shall be installed to the application's AppData directory.", Separator = ';')]
    public ICollection<string> AppDataComponents { get; init; }

    [Option("installDirFiles", HelpText = "Files which shall be installed to the application's install directory.", Separator = ';')]
    public ICollection<string> InstallDirComponents { get; init; }

    public Uri OriginRootUri => new(Origin, UriKind.Absolute);
}