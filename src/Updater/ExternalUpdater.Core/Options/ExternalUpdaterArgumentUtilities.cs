using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AnakinRaW.ExternalUpdater.Options;

public static class ExternalUpdaterArgumentUtilities
{ 
    public static string ToArgs(this ExternalUpdaterOptions option)
    {
        return Parser.Default.FormatCommandLine(option, s =>
        {
            s.SkipDefault = true;
        });
    }

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