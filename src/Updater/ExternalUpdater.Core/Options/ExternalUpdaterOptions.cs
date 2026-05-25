using CommandLine;

namespace AnakinRaW.ExternalUpdater.Options;

/// <summary>Represents the command-line options shared by all external updater verbs.</summary>
public record ExternalUpdaterOptions
{
    /// <summary>Gets the absolute path of the application to start after the external updater has finished its work.</summary>
    /// <remarks>
    /// <para>
    /// The verb <c>restart</c> requires the value to be non-<see langword="null"/>.
    /// </para>
    /// <para>
    /// The verb <c>update</c> treats a <see langword="null"/> value as "apply the update and exit".
    /// </para>
    /// </remarks>
    /// <value>The absolute path of the executable to launch, or <see langword="null"/> to skip the relaunch step.</value>
    [Option("appToStart", Required = false, HelpText = "The absolute path of the application to start after the operation. Omit to skip the relaunch step.")]
    public string? AppToStart { get; init; }

    /// <summary>Gets the command-line arguments, encoded as Base64, that are forwarded to <see cref="AppToStart"/> when it is launched.</summary>
    /// <value>The Base64-encoded argument string, or <see langword="null"/> when no arguments are forwarded.</value>
    [Option("appToStartArgs", Required = false, HelpText = "The arguments to use when launching 'appToStart' encoded as base64.")]
    public string? AppToStartArguments { get; init; } = null;

    /// <summary>Gets a value that indicates whether <see cref="AppToStart"/> should be launched with elevated privileges.</summary>
    /// <value>
    /// <see langword="true"/> if the application is launched with elevated privileges;
    /// otherwise, <see langword="false"/>.
    /// The default is <see langword="false"/>.
    /// </value>
    [Option("elevate", Required = false, HelpText = "The application shall be started with higher rights.")]
    public bool Elevate { get; init; }

    /// <summary>Gets the maximum time, in seconds, the external updater waits for the process identified by <see cref="Pid"/> to terminate.</summary>
    /// <value>The timeout in seconds. The default is 10.</value>
    [Option("timeout", Required = false, HelpText = "The maximum time in seconds to wait for the specified process to terminate.", Default = 10)]
    public int Timeout { get; init; }

    /// <summary>Gets the process identifier of the process to wait for before the external updater starts its work.</summary>
    /// <value>The process identifier, or <see langword="null"/> when no process should be awaited.</value>
    [Option("pid", Required = false, HelpText = "The PID of the process to wait until terminated.")]
    public int? Pid { get; init; }

    /// <summary>Gets the directory in which the external updater writes its log file.</summary>
    /// <value>The absolute path of the logging directory, or <see langword="null"/> to log next to the executable.</value>
    [Option("loggingDir", HelpText = "The location where log files get placed.", Default = null, Required = false)]
    public string? LoggingDirectory { get; init; }
}