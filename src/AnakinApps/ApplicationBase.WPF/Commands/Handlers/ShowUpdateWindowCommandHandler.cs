using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.ViewModels;
using AnakinRaW.CommonUtilities.Windows;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Commands.Handlers;

internal class ShowUpdateWindowCommandHandler : AsyncCommandHandlerBase, IShowUpdateWindowCommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionManager _connectionManager;
    private readonly IMetadataExtractor _metadataExtractor;

    public ShowUpdateWindowCommandHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
    }

    public override async Task HandleAsync()
    {
        await ExtractAssemblies();

        // Singletone instance of this view model drastically increases closing/cancellation complexity.
        // Creating a new model for each request should be good enough. 
        var viewModel = new UpdateWindowViewModel(_serviceProvider);
        await _serviceProvider.GetRequiredService<IModalWindowService>().ShowModal(viewModel);
    }

    protected override bool CanHandle()
    {
        return _connectionManager.HasInternetConnection();
    }

    private async Task ExtractAssemblies()
    {
        var env = _serviceProvider.GetRequiredService<IApplicationEnvironment>();
        await _serviceProvider.GetRequiredService<IResourceExtractor>()
            .ExtractAsync(ExternalUpdaterConstants.AppUpdaterModuleName, env.ApplicationLocalPath, ShouldOverwriteUpdater);
    }

    private bool ShouldOverwriteUpdater(string filePath, Stream assemblyStream)
    {
        if (!Version.TryParse(FileVersionInfo.GetVersionInfo(filePath).FileVersion, out var installedVersion))
            return true;
        var streamVersion = _metadataExtractor.InformationFromStream(assemblyStream).FileVersion;
        if (streamVersion is null)
            return true;
        return streamVersion > installedVersion;
    }
}