using System;
using Flurl;

namespace AnakinRaW.ApplicationBase;

public static class ApplicationVariablesKeys
{
    public const string AppData = "AppData";
    public const string AppFileName = "AppFileName";
}

public class BranchUriBuilder
{
    private readonly Url _appRootUri;

    public BranchUriBuilder(Url appRootUri)
    {
        _appRootUri = appRootUri;
    }

    public Uri Build(string branchName)
    {
        return _appRootUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri();
    }
}