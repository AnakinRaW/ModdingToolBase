using System;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

internal sealed class ManifestDownloadException : ManifestException
{
    public ManifestDownloadException(string message) : base(message)
    {
    }

    public ManifestDownloadException(string message, Exception inner) : base(message, inner)
    {
        
    }
}