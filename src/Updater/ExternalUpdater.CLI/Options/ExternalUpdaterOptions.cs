using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

public record ExternalUpdaterOptions
{
    [Option("appToStart", Required = true, HelpText = "The absolute path of the application to start.")]
    public required string AppToStart { get; init; }

    [Option("elevate", Required = false, HelpText = "The application shall be started with higher rights.")]
    public bool Elevate { get; init; }

    [Option("timeout", Required = false, HelpText = "The maximum time in seconds to wait for the specified process to terminate.", Default = 10)]
    public int Timeout { get; init; }

    [Option("pid", Required = false, HelpText = "The PID of the process to wait until terminated.")]
    public int? Pid { get; init; }

    [Option("loggingDir", HelpText = "The location where log files get placed.", Default = null, Required = false)]
    public string? LoggingDirectory { get; init; }
}