﻿using System;
using System.Collections.Generic;
using CommandLine;

namespace AnakinRaW.ApplicationManifestCreator;

internal class ManifestCreatorOptions
{
    [Option('a', "applicationFile", Required = true, HelpText = "The file of the application to create the manifest for.")]
    public string ApplicationFile { get; init; } = null!;

    [Option('o', "output", Default = "", Required = false, HelpText = "File path of the manifest file.")]
    public string OuputPath { get; init; } = string.Empty;

    [Option("origin", Required = true, HelpText = "Root url where all components are expected to be deployed. Must be an absolute uri.")]
    public string Origin { get; init; } = null!;

    [Option("appDataFiles", HelpText = "Files which shall be installed to the application's AppData directory.",
        Separator = ';')]
    public ICollection<string> AppDataComponents { get; init; } = Array.Empty<string>();

    [Option("installDirFiles", HelpText = "Files which shall be installed to the application's install directory.", Separator = ';')]
    public ICollection<string> InstallDirComponents { get; init; } = Array.Empty<string>();

    public Uri OriginRootUri => new(Origin, UriKind.Absolute);
}