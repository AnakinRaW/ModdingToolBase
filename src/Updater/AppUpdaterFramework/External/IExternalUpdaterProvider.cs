using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.External;

/// <summary>
/// A service that provides methods to make an external updater available on disk and to retrieve integrity information about it.
/// </summary>
public interface IExternalUpdaterProvider
{
    /// <summary>
    /// Retrieves the expected integrity information of the external updater.
    /// </summary>
    /// <remarks>
    /// Implementations of this method should ensure to always return integrity information other than <see cref="ComponentIntegrityInformation.None"/>
    /// if the application uses an external updater, even if the updater is not currently available on disk.
    /// If the application does not use an external updater, this method should throw an <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <returns>
    /// A <see cref="ComponentIntegrityInformation"/> instance containing the hash and hash type
    /// of the external updater.
    /// </returns>
    /// <exception cref="InvalidOperationException">The application does not use the external updater.</exception>
    ComponentIntegrityInformation GetIntegrity();

    /// <summary>
    /// Ensures that the external updater is available on disk.
    /// </summary>
    /// <param name="force">
    /// When <see langword="true"/>, overwrite any on-disk updater unconditionally, regardless of its version.
    /// </param>
    void EnsureAvailable(bool force = false);
}
