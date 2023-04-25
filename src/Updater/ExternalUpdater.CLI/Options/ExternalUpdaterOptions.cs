using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

public record ExternalUpdaterOptions
{
    private IReadOnlyList<string>? _originalArguments = null;

    [Option('s', "startProcess", Required = true, HelpText = "The absolute path of the application to start.")]
    public required string AppToStart { get; init; }

    [Option('e', "elevate", Required = false, HelpText = "The application shall be started with higher rights.")]
    public bool Elevate { get; init; }

    [Option('t', "timeout", Required = false, HelpText = "The maximum time in seconds to wait for the specified process to terminate.", Default = 10)]
    public int Timeout { get; init; }

    [Option('p', "pid", Required = false, HelpText = "The PID of the process to wait until terminated.")]
    public int? Pid { get; init; }

    [Option("originalArgs", Required = false, HelpText = "The original arguments of the process that shall be restarted encoded in base64 JSON string[]")]
    public string? OriginalArgumentData { get; init; }

    public IReadOnlyList<string> OriginalArguments
    {
        get
        {
            if (_originalArguments is null)
            {
                if (OriginalArgumentData is null)
                    _originalArguments = Array.Empty<string>();
                else
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(OriginalArgumentData));
                    var originalArgs = JsonSerializer.Deserialize<IEnumerable<string>>(json);
                    if (originalArgs is null)
                        _originalArguments = Array.Empty<string>();
                    else 
                        _originalArguments = new List<string>(originalArgs);
                }
            }
            return _originalArguments;
        }
    }

}