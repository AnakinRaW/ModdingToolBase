using System.Diagnostics;
using System.IO.Abstractions;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Services;

/// <summary>Launches the external updater executable as a separate process.</summary>
public interface IExternalUpdaterLauncher
{
    /// <summary>Starts the external updater process with the given options.</summary>
    /// <param name="updater">The external updater executable to launch.</param>
    /// <param name="options">The options that configure the updater run.</param>
    /// <returns>The started process.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="updater"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.IO.FileNotFoundException"><paramref name="updater"/> does not exist on disk.</exception>
    Process Start(IFileInfo updater, ExternalUpdaterOptions options);
}