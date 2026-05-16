using AnakinRaW.AppUpdaterFramework.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// DI extension methods for the JSON manifest format.
/// </summary>
public static class JsonManifestServiceExtensions
{
    /// <summary>
    /// Registers <see cref="JsonManifestVerifier"/> as the framework's <see cref="ManifestVerifierBase"/>.
    /// Without this call, <c>AddUpdateFramework</c>'s default <c>NullManifestVerifier</c> reports every
    /// manifest as missing a signature, which fails under <see cref="SignaturePolicy.Required"/>.
    /// </summary>
    public static IServiceCollection AddJsonManifestVerifier(this IServiceCollection services)
    {
        services.AddSingleton<ManifestVerifierBase>(sp => new JsonManifestVerifier(sp));
        return services;
    }
}
