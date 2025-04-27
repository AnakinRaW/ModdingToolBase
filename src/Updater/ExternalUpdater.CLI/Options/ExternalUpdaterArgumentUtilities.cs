using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.Json;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ExternalUpdater.Options;

public static class ExternalUpdaterArgumentUtilities
{
    public static ExternalUpdaterOptions FromArgs(string args)
    {
        var splitArgs = args.SplitArgs();
        return FromArgs(splitArgs);
    }

    public static ExternalUpdaterOptions FromArgs(IReadOnlyList<string> args)
    {
        ExternalUpdaterOptions result = null!;
        
        result = Parser.Default.ParseArguments<RestartOptions, UpdateOptions>(args)
            .MapResult(
                (RestartOptions opts) => result = opts,
                (UpdateOptions opts) => result = opts,
                errors => throw new ArgumentException($"The provided args cannot be parsed: {errors.FirstOrDefault()}"));

        return result;
    }

    public static string ToArgs(this ExternalUpdaterOptions option)
    {
        return Parser.Default.FormatCommandLine(option, config =>
        {
            config.SkipDefault = true;
        });
    }

    public static string? GetCurrentApplicationCommandLineForPassThrough()
    {
        var currentCommandLineArgs = Environment.GetCommandLineArgs();

        // If command args are empty or only contain the executable name (which is always at index 0), there is nothing to return. 
        if (currentCommandLineArgs.Length <= 1)
            return null;

        Process.Start()

        // If the current command line args do not contain the ExternalUpdaterResultOptions, we can return all args after the executable name.
        if (!ExternalUpdaterResultOptions.TryParse(currentCommandLineArgs, out _))
            return new List<string>(currentCommandLineArgs.Skip(1));


        var args = new List<string>(currentCommandLineArgs.Length - 1);

        var externalResultExpected = false;
        for (var i = 1; i < currentCommandLineArgs.Length; i++)
        {
            var arg = currentCommandLineArgs[i];

            if (externalResultExpected)
            {
                Debug.Assert(Enum.TryParse(arg, out ExternalUpdaterResult _));
                externalResultExpected = false;
                continue;
            }

            if (arg.Equals(ExternalUpdaterResultOptions.RawOptionString))
            {
                externalResultExpected = true;
                continue;
            }

            args.Add(arg);
        }

        //return [];

        return args;
    }

    public static ExternalUpdaterOptions WithCurrentData(
        this ExternalUpdaterOptions options, 
        string appToStart,
        string? appToStartArgs,
        int? pid,
        string? loggingDirectory,
        IServiceProvider serviceProvider)
    {
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        appToStart = fileSystem.Path.GetFullPath(appToStart);
        
        if (!string.IsNullOrEmpty(loggingDirectory))
            loggingDirectory = fileSystem.Path.GetFullPath(loggingDirectory!);

        if (options is UpdateOptions updateOptions)
        {
            if (ReplaceUpdateItemsWithCurrentApp(updateOptions, appToStart, out var updateItems, serviceProvider))
                options = updateOptions with { UpdateFile = null, Payload = updateItems!.ToPayload() };
        }

        return options with
        {
            AppToStart = appToStart, 
            AppToStartArguments = appToStartArgs,
            Pid = pid, 
            LoggingDirectory = loggingDirectory
        };
    }


    internal static string ToPayload(this IEnumerable<UpdateInformation> updateInformation)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(updateInformation.Serialize()));
    }

    public static string Serialize(this IEnumerable<UpdateInformation> updateInformation)
    {
        return JsonSerializer.Serialize(updateInformation);
    }

    private static bool ReplaceUpdateItemsWithCurrentApp(
        UpdateOptions oldOptions, 
        string currentAppPath, 
        out IList<UpdateInformation>? updateInformation, 
        IServiceProvider serviceProvider)
    {
        var updated = false;
        updateInformation = null;

        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        currentAppPath = fileSystem.Path.GetFullPath(currentAppPath);
        var oldAppPath = fileSystem.Path.GetFullPath(oldOptions.AppToStart);

        if (currentAppPath.Equals(oldAppPath))
            return false;

        var oldUpdateItems = oldOptions.GetUpdateInformation(serviceProvider);
        var newItems = new List<UpdateInformation>(oldUpdateItems.Count);

        foreach (var item in oldUpdateItems)
        {
            var newItem = item;

            var update = item.Update;
            var backup = item.Backup;

            if (update?.Destination is not null && update.Destination.Equals(oldAppPath))
            {
                newItem = newItem with { Update = update with { Destination = currentAppPath } };
                updated = true;
            }

            if (backup?.Destination is not null && backup.Destination.Equals(oldAppPath))
            {
                newItem = newItem with { Backup = backup with { Destination = currentAppPath } };
                updated = true;
            }

            newItems.Add(newItem);
        }

        if (updated)
            updateInformation = newItems;

        return updated;
    }
}