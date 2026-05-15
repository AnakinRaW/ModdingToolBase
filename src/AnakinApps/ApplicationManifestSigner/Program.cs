using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;

namespace AnakinRaW.ApplicationManifestSigner;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<SignerOptions>(args)
            .MapResult(Run, _ => Task.FromResult(1));
    }

    private static async Task<int> Run(SignerOptions options)
    {
        var services = ConfigureServices();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Signer");

        try
        {
            var manifest = await ReadManifestAsync(options.ManifestPath);
            using var key = SigningKey.LoadFromPfx(options.PfxPath, options.PfxPassword);

            var signer = new ManifestSigner(
                services.GetRequiredService<IHashingService>(),
                new SigningConfiguration());
            var signed = signer.Sign(manifest, key);

            await WriteManifestAsync(signed, options.OutputPath ?? options.ManifestPath);
            logger?.LogInformation("Signed manifest written to {Path}", options.OutputPath ?? options.ManifestPath);
            return 0;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Signing failed: {Message}", ex.Message);
            return ex.HResult;
        }
    }

    private static async Task<ApplicationManifest> ReadManifestAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync<ApplicationManifest>(stream, ManifestJsonOptions.Default)
            ?? throw new InvalidDataException($"Manifest at '{path}' deserialized as null.");
        return manifest;
    }

    private static async Task WriteManifestAsync(ApplicationManifest manifest, string path)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, ManifestJsonOptions.Default);
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddLogging(b => b.AddConsole());
        return services.BuildServiceProvider();
    }
}
