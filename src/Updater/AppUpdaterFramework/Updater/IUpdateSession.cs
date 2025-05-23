﻿using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using System;

namespace AnakinRaW.AppUpdaterFramework.Updater;

public interface IUpdateSession
{
    event EventHandler<UpdateProgressEventArgs> DownloadProgress;

    event EventHandler<UpdateProgressEventArgs> InstallProgress;

    ProductReference Product { get; }
    
    void Cancel();
}