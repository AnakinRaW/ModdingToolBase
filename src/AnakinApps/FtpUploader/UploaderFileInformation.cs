using System.Collections.Generic;
using System.IO.Abstractions;

namespace AnakinRaW.FtpUploader;

internal class UploaderFileInformation
{
    public IFileInfo Manifest { get; }

    public IEnumerable<IFileInfo> ApplicationFiles { get; }

    public IFileInfo? BranchLookup { get; init; }

    public UploaderFileInformation(IFileInfo manifest, IEnumerable<IFileInfo> applicationFiles, IFileInfo? branchLookup = null)
    {
        Manifest = manifest;
        ApplicationFiles = applicationFiles;
        BranchLookup = branchLookup;
    }
}