﻿using System.ComponentModel;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
public class DesignerErrorStateViewModel : IErrorStateViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;


    public string? Version { get; } = "1.2.3.4";

    public string ErrorMessage { get; } =
        "This is an error message indicating something went wrong with the update process.";

    public Task InitializeAsync()
    {
        throw new System.NotImplementedException();
    }
}