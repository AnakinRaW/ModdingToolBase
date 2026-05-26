using System;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// DI extension methods for the JSON manifest format.
/// </summary>
public static class JsonManifestServiceExtensions
{
    /// <summary>
    /// Registers <see cref="JsonManifestLoader"/> as a singleton concrete type. Hosts pass the
    /// resolved instance to their <c>BranchManagerBase</c> subclass via constructor parameter.
    /// </summary>
    public static IServiceCollection AddJsonManifestLoader(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        services.AddSingleton(sp => new JsonManifestLoader(sp));
        return services;
    }
}
