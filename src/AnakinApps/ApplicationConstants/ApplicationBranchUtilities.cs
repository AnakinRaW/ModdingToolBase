﻿using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager;
using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.ApplicationBase;

public class ApplicationBranchUtilities
{
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadManager _downloadManager;

    public ICollection<Uri> Mirrors { get; }
    
    public ApplicationBranchUtilities(Uri appRootUri, IServiceProvider serviceProvider) : this(new List<Uri>{appRootUri}, serviceProvider)
    {
    }

    public ApplicationBranchUtilities(ICollection<Uri> mirrors, IServiceProvider serviceProvider)
    {
        Requires.NotNull(mirrors, nameof(mirrors));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _downloadManager = serviceProvider.GetRequiredService<IDownloadManager>();
        Mirrors = mirrors;
    }

    public async Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        var lookupUri = Mirrors.Select(GetBranchLookupUrl);
        var branchesData = await DownloadFromMirrors(lookupUri);
        var branchNames = Encoding.UTF8.GetString(branchesData).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (!branchNames.Any())
            return Enumerable.Empty<ProductBranch>();

        var branches = new List<ProductBranch>();
        foreach (var name in branchNames)
        {
            var isPrerelease = !name.Equals(ApplicationConstants.StableBranchName, StringComparison.InvariantCultureIgnoreCase);
            branches.Add(new ProductBranch(name, BuildManifestUris(name), isPrerelease));
        }
        return branches;
    }

    private async Task<byte[]> DownloadFromMirrors(IEnumerable<Url> downloadLocations)
    {
        foreach (var requestUri in downloadLocations)
        {
            try
            {
                using var ms = new MemoryStream();
                await _downloadManager.DownloadAsync(requestUri.ToUri(), ms, null);
                return ms.ToArray();
            }
            catch (Exception e) when (e is HttpRequestException or DownloadFailedException)
            {
                // Ignore and try next mirror
            }
        }
        throw new InvalidOperationException("Unable to download");
    }

    private static Url GetBranchLookupUrl(Uri baseUri)
    {
        return baseUri.AppendPathSegment(ApplicationConstants.BranchLookupFileName);
    }

    public ICollection<Uri> BuildManifestUris(string branchName)
    {
        return Mirrors.Select(mirrorUri => mirrorUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri()).ToList();
    }

    internal Url BuildComponentUri(Uri baseUri, string branchName, string fileName)
    {
        var pathWithBranch = baseUri.AppendPathSegment(branchName);
#if NETSTANDARD2_0
        var basePath = pathWithBranch.ToUri().AbsoluteUri.TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar);
#else
        var basePath = _fileSystem.Path.TrimEndingDirectorySeparator(pathWithBranch.ToUri().AbsoluteUri);
#endif


        // We cannot use Url.Combine (cause of unwanted encoding of space character) or Path.Combine (cause of using backslash)
        // Is this a security issue???
        return new Url($"{basePath}/{fileName}");
    }
}