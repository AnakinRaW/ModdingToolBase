using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AnakinRaW.ExternalUpdater.Options;

/// <summary>Provides helpers for converting between <see cref="ExternalUpdaterOptions"/> instances and the command-line argument strings the external updater consumes.</summary>
public static class ExternalUpdaterArgumentUtilities
{
    /// <summary>Formats the given options as a command-line argument string suitable for invoking the external updater.</summary>
    /// <param name="option">The options to format.</param>
    /// <returns>The command-line argument string for <paramref name="option"/>, with default values omitted.</returns>
    public static string ToArgs(this ExternalUpdaterOptions option)
    {
        return Parser.Default.FormatCommandLine(option, s =>
        {
            s.SkipDefault = true;
        });
    }

    /// <summary>
    /// Captures the current process's command-line arguments, excluding the executable itself
    /// and any previous external updater result switch, and returns them as a Base64-encoded string.
    /// </summary>
    /// <remarks>
    /// The <see cref="ExternalUpdaterResultOptions.RawOptionString"/> switch and its value are stripped
    /// so that the new updater run can attach its own result without duplicating the previous one.
    /// </remarks>
    /// <returns>
    /// The Base64-encoded argument string suitable for use as <see cref="ExternalUpdaterOptions.AppToStartArguments"/>,
    /// or <see langword="null"/> if there are no arguments to pass through.
    /// </returns>
    public static string? GetCurrentApplicationCommandLineForPassThroughAsBase64()
    {
        var currentCommandLineArgs = Environment.CommandLine.SplitArgs(true);

        // If command args are empty or only contain the executable name (which is always at index 0), there is nothing to return.
        if (currentCommandLineArgs.Length <= 1)
            return null;

        var externalResultExpected = false;
        var actualArgs = new List<string>(currentCommandLineArgs.Length - 1);

        // Starting from index 1, because we don't want to include the executable itself.
        for (var i = 1; i < currentCommandLineArgs.Length; i++)
        {
            var arg = currentCommandLineArgs[i];

            if (externalResultExpected)
            {
                Debug.Assert(Enum.TryParse(arg, out ExternalUpdaterResult _));
                externalResultExpected = false;
                continue;
            }

            if (string.IsNullOrEmpty(arg))
                continue;

            if (arg.Equals(ExternalUpdaterResultOptions.RawOptionString))
            {
                externalResultExpected = true;
                continue;
            }

            actualArgs.Add(arg);
        }

        if (actualArgs.Count == 0)
            return null;

        var argsString = string.Join(" ", actualArgs);
        return Convert.ToBase64String(Encoding.Default.GetBytes(argsString));
    }
}