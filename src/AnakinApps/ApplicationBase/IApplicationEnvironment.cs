using System;
using System.Collections.Generic;
using System.IO.Abstractions;

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
}