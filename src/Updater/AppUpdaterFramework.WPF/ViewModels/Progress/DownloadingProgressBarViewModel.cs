﻿using System;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.Utilities;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Progress;

public sealed class DownloadingProgressBarViewModel : ProgressBarViewModel
{
    public override string? LeftHeaderText {
        get
        {
            var progressInformation = ProgressInformation;
            if (progressInformation is null)
                return "Starting download operation";
            if (progressInformation.Progress >= 1.0)
                return null;
            if (progressInformation.Progress >= 1.0)
                return "Downloaded";
            return
                $"Downloading: {UpdateUtilities.ToHumanReadableSize(progressInformation.ProgressInfo.DownloadedSize)} of {UpdateUtilities.ToHumanReadableSize(progressInformation.ProgressInfo.TotalSize)}";
        }
    }

    public override string? RightHeaderText
    {
        get
        {
            var progressInformation = ProgressInformation;
            if (progressInformation is null)
                return null;
            if (progressInformation.Progress >= 1.0)
                return null;
            var speed = progressInformation.ProgressInfo.DownloadSpeed;
            return speed > 0 ? $"( {UpdateUtilities.ToHumanReadableSize(speed)}/sec )" : null;
        }
    }

    public override string? FooterText => null;

    public DownloadingProgressBarViewModel(IUpdateSession updateSession, IServiceProvider serviceProvider) 
        : base(updateSession, nameof(IUpdateSession.DownloadProgress), serviceProvider)
    {
        if (updateSession == null)
            throw new ArgumentNullException(nameof(updateSession));
    }
}