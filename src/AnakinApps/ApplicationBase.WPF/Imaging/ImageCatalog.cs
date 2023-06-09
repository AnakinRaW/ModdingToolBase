﻿using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Wpf.Imaging;

namespace AnakinRaW.ApplicationBase.Imaging;

internal class ImageCatalog : ImmutableImageCatalog
{
    private static readonly Lazy<ImageCatalog> LazyConstruction = new(() => new ImageCatalog());
    public static ImageCatalog Instance => LazyConstruction.Value;

    public static ImageDefinition TrooperDefinition => new()
    {
        Kind = ImageFileKind.Png,
        ImakgeKey = ImageKeys.Trooper,
        Source = ResourcesUriCreator.Create("sadTrooper", ImageFileKind.Png),
        CanTheme = false
    };

    public static ImageDefinition GithubDefinition => new()
    {
        Kind = ImageFileKind.Png,
        ImakgeKey = ImageKeys.Github,
        Source = ResourcesUriCreator.Create("GitHub_Mark_32px", ImageFileKind.Png),
        CanTheme = true
    };

    public static ImageDefinition UpdateIconDefinition => new()
    {
        Kind = ImageFileKind.Xaml,
        ImakgeKey = ImageKeys.UpdateIcon,
        Source = ResourcesUriCreator.Create("StatusUpdateAvailable", ImageFileKind.Xaml),
        CanTheme = true
    };

    public static ImageDefinition HelpIconDefinition => new()
    {
        Kind = ImageFileKind.Xaml,
        ImakgeKey = ImageKeys.StatusHelpIcon,
        Source = ResourcesUriCreator.Create("StatusHelp", ImageFileKind.Xaml),
        CanTheme = true
    };

    public static ImageDefinition VaderDefinition => new()
    {
        Kind = ImageFileKind.Png,
        ImakgeKey = ImageKeys.Vader,
        Source = ResourcesUriCreator.Create("vader", ImageFileKind.Png),
        CanTheme = false
    };

    public static ImageDefinition PalpatineDefinition => new()
    {
        Kind = ImageFileKind.Png,
        ImakgeKey = ImageKeys.Palpatine,
        Source = ResourcesUriCreator.Create("senat", ImageFileKind.Png),
        CanTheme = false
    };

    public static ImageDefinition SwPulpDefinition => new()
    {
        Kind = ImageFileKind.Png,
        ImakgeKey = ImageKeys.SwPulp,
        Source = ResourcesUriCreator.Create("kill", ImageFileKind.Png),
        CanTheme = false
    };


    public static readonly IEnumerable<ImageDefinition> Definitions = new List<ImageDefinition>
    {
        GithubDefinition, 
        TrooperDefinition,
        UpdateIconDefinition,
        HelpIconDefinition,
        VaderDefinition,
        PalpatineDefinition, 
        SwPulpDefinition
    };

    private ImageCatalog() : base(Definitions)
    {
    }
}