using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Utilities;

public class CosturaResourceExtractor
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly Assembly _appAssembly;

    private readonly IReadOnlyCollection<string> _assemblyResourceNames;

    public CosturaResourceExtractor(Assembly assembly, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _appAssembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        _assemblyResourceNames = assembly.GetManifestResourceNames();
    }

    public bool Contains(string resourceName)
    {
        return TryGetResourceName(resourceName, out _, out _);
    }

    public bool TryGetResourceName(string resourceName, [NotNullWhen(true)] out string? actualResourceName, out bool compressed)
    {
        var nameStart = $"costura.{resourceName.ToLowerInvariant()}";
        foreach (var resource in _assemblyResourceNames)
        {
            if (resource.StartsWith(nameStart))
            {
                compressed = resource.EndsWith(".compressed");
                actualResourceName = resource;
                return true;
            }
        }

        actualResourceName = null;
        compressed = false;
        return false;
    }

    public void Extract(string resourceName, string fileDirectory, Func<string, Stream, bool> shouldOverwrite)
    {
        ExtractAsync(resourceName, fileDirectory, shouldOverwrite).GetAwaiter().GetResult();
    }

    public void Extract(string resourceName, string fileDirectory)
    {
        Extract(resourceName, fileDirectory, (_, _) => true);
    }

    public async Task ExtractAsync(string resourceName, string fileDirectory)
    {
        await ExtractAsync(resourceName, fileDirectory, (_, _) => true);
    }

    public async Task ExtractAsync(string resourceName, string fileDirectory, Func<string, Stream, bool> shouldOverwrite)
    {
        using var assemblyStream = await GetResourceStreamAsync(resourceName);
        await ExtractAsync(assemblyStream, resourceName, fileDirectory, shouldOverwrite);
    }

    public async ValueTask<Stream> GetResourceStreamAsync(string resourceName)
    {
        if (!TryGetResourceName(resourceName, out var actualResourceName, out var compressed))
            throw new IOException($"Could not find embedded resource '{resourceName}'");

        var assemblyResourceStream = _appAssembly.GetManifestResourceStream(actualResourceName);
        if (assemblyResourceStream is null)
            throw new InvalidOperationException($"Assembly stream for '{resourceName}' was null!");

        if (!compressed) 
            return assemblyResourceStream;

        var decompressedStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(assemblyResourceStream, CompressionMode.Decompress))
            await deflateStream.CopyToAsync(decompressedStream);
        decompressedStream.Position = 0;
        return decompressedStream;
    }

    private async Task ExtractAsync(Stream resourceStream, string assemblyName, string fileDirectory, Func<string, Stream, bool> shouldOverwrite)
    {
        if (!_fileSystem.Directory.Exists(fileDirectory))
            throw new DirectoryNotFoundException("The requested destination folder does not exist.");

        var filePath = _fileSystem.Path.Combine(fileDirectory, assemblyName);
        try
        {
            if (_fileSystem.File.Exists(filePath) && !shouldOverwrite(filePath, resourceStream))
                return;

            _logger?.LogDebug($"Writing file: '{filePath}'");

            resourceStream.Position = 0;

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await resourceStream.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            throw new IOException("Error writing necessary files to disk!", ex);
        }
    }
}