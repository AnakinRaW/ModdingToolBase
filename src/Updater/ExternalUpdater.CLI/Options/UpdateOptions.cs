﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ExternalUpdater.Options;

[Verb("update", HelpText = "Updates the given application and restarts it")]
public sealed record UpdateOptions : ExternalUpdaterOptions
{
    private IReadOnlyCollection<UpdateInformation>? _updateItems;
    
    [Option("updatePayload", Required = false, HelpText = "Payload in base64 format which is required for the update")]
    public string? Payload { get; init; }

    [Option("updateFile", Required = false, HelpText = "JSON file where update data is stored.")]
    public string? UpdateFile { get; init; }

    public Task<IReadOnlyCollection<UpdateInformation>> GetUpdateInformationAsync(IServiceProvider serviceProvider)
    {
        return Task.Run(() => GetUpdateInformation(serviceProvider));
    }

    internal IReadOnlyCollection<UpdateInformation> GetUpdateInformation(IServiceProvider serviceProvider)
    {
        return _updateItems ??= CreateUpdateInformation(serviceProvider);
    }

    private IReadOnlyCollection<UpdateInformation> CreateUpdateInformation(IServiceProvider serviceProvider)
    {
        var information = GetFromFile(serviceProvider);
        if (information is not null)
            return information;
        information = GetFromPayload();
        return information ?? Array.Empty<UpdateInformation>();
    }

    private IReadOnlyCollection<UpdateInformation>? GetFromFile(IServiceProvider serviceProvider)
    {
        var fs = serviceProvider.GetRequiredService<IFileSystem>();
        if (string.IsNullOrEmpty(UpdateFile) || !fs.File.Exists(UpdateFile))
            return null;
        using var fileStream = fs.FileStream.New(UpdateFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<IReadOnlyCollection<UpdateInformation>>(fileStream, JsonSerializerOptions.Default);

    }

    private IReadOnlyCollection<UpdateInformation>? GetFromPayload()
    {
        if (string.IsNullOrEmpty(Payload))
            return null;
        var decoded = Convert.FromBase64String(Payload!);
        using var ms = new MemoryStream(decoded, false);
        return JsonSerializer.Deserialize<IReadOnlyCollection<UpdateInformation>>(ms, JsonSerializerOptions.Default);
    }
}