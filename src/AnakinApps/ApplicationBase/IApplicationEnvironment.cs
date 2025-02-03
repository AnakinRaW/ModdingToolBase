using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;

namespace AnakinRaW.ApplicationBase;

public interface IApplicationEnvironment
{
    ApplicationAssemblyInfo AssemblyInfo { get; }
    
    string ApplicationName { get; }

    string ApplicationLocalPath { get; }

    IDirectoryInfo ApplicationLocalDirectory { get; }

    Uri? RepositoryUrl { get; }

    ICollection<Uri> UpdateMirrors { get; }

    string ApplicationRegistryPath { get; }

    DownloadManagerConfiguration UpdateDownloadManagerConfiguration { get; }
}