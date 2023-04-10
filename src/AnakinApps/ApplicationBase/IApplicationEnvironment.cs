using System;
using System.IO.Abstractions;

namespace AnakinRaW.ApplicationBase;

public interface IApplicationEnvironment
{
    ApplicationAssemblyInfo AssemblyInfo { get; }
    
    string ApplicationName { get; }

    string ApplicationLocalPath { get; }

    IDirectoryInfo ApplicationLocalDirectory { get; }

    Uri? RepositoryUrl { get; }

    Uri UpdateRootUrl { get; }

    string ApplicationRegistryPath { get; }
}