﻿using System;
using AnakinRaW.AppUpdaterFramework.Updater;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Progress;

public sealed class InstallingProgressBarViewModel : ProgressBarViewModel
{
    public override string LeftHeaderText {
        get
        {
            var progressInformation = ProgressInformation;
            if (progressInformation is null)
                return "Starting install operation";
            return progressInformation.Progress >= 0.99
                ? "Installing... This might take a while."
                : $"Installing: component {progressInformation.DetailedProgress.CurrentComponent} of {progressInformation.DetailedProgress.TotalComponents}";
        }
    }

    public override string? RightHeaderText => null;

    public override string? FooterText => ProgressInformation?.Component;

    public InstallingProgressBarViewModel(IUpdateSession updateSession, IServiceProvider serviceProvider) 
        : base(updateSession, nameof(IUpdateSession.InstallProgress), serviceProvider)
    {
        if (updateSession == null) 
            throw new ArgumentNullException(nameof(updateSession));
    }
}