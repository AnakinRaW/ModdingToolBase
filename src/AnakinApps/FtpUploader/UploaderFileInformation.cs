using System.Collections.Generic;
using System.IO.Abstractions;

namespace AnakinRaW.FtpUploader;

internal class UploaderFileInformation(
    IFileInfo manifest,
    IEnumerable<IFileInfo> applicationFiles,
    IFileInfo? branchLookup = null)
{
    public IFileInfo Manifest { get; } = manifest;

    public IEnumerable<IFileInfo> ApplicationFiles { get; } = applicationFiles;

    public IFileInfo? BranchLookup { get; init; } = branchLookup;
}