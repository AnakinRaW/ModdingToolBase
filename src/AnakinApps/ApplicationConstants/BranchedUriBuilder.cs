using System;
using Flurl;

namespace AnakinRaW.ApplicationBase;

public class BranchedUriBuilder
{
    private readonly Url _appRootUri;

    public BranchedUriBuilder(Url appRootUri)
    {
        _appRootUri = appRootUri;
    }

    public Uri BuildManifestUri(string branchName)
    {
        return _appRootUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri();
    }

    public Uri BuildComponentUri(string branchName, string fileName)
    {
        return _appRootUri.AppendPathSegments(branchName, fileName).ToUri();
    }
}