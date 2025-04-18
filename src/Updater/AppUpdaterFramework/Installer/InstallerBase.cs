﻿using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal abstract class InstallerBase : IInstaller
{
    public event EventHandler<ComponentProgressEventArgs>? Progress;

    protected readonly ILogger? Logger;

    protected InstallerBase(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public InstallResult Install(IInstallableComponent component, IFileInfo? source, IReadOnlyDictionary<string, string> variables, CancellationToken token = default)
    {
        return ExecuteInstallerAction(component, source, InstallAction.Install, variables, token);
    }

    public InstallResult Remove(IInstallableComponent component, IReadOnlyDictionary<string, string> variables, CancellationToken token = default)
    {
        return ExecuteInstallerAction(component, null, InstallAction.Remove, variables, token);
    }

    protected abstract InstallResult RemoveCore(IInstallableComponent component, IReadOnlyDictionary<string, string> variables, CancellationToken token);

    protected abstract InstallResult InstallCore(IInstallableComponent component, IFileInfo source, IReadOnlyDictionary<string, string> variables, CancellationToken token);


    private InstallResult ExecuteInstallerAction(IInstallableComponent component, IFileInfo? source, InstallAction action, IReadOnlyDictionary<string, string> variables, CancellationToken token)
    {
        try
        {
            OnProgress(component, 0.0);
            try
            {
                Logger?.LogInformation($"Started: {action}ing {component.GetDisplayName()}");
                switch (action)
                {
                    case InstallAction.Install:
                        return InstallCore(component, source!, variables, token);
                    case InstallAction.Remove:
                        return RemoveCore(component, variables, token);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            }
            finally
            {
                Logger?.LogInformation($"Completed: {action}ing {component.GetDisplayName()}");
            }
        }
        catch (OperationCanceledException)
        {
            Logger?.LogInformation($"User canceled during component {action}.");
            return InstallResult.Cancel;
        }
        catch (UnauthorizedAccessException e)
        {
            LogFailure(component, action, e.ToString());
            return InstallResult.FailureElevationRequired;
        }
        catch (Exception e)
        {
            LogFailure(component, action, e.ToString());
            return InstallResult.Failure;
        }
        finally
        {
            OnProgress(component, 1.0);
        }
    }

    protected InstallResult ExecuteWithInteractiveRetry(
        IInstallableComponent component,
        Func<InstallOperationResult> action,
        Func<InstallOperationResult, InstallerInteractionResult> interaction,
        CancellationToken token)
    {
        InstallResult result;
        var retry = false;
        do
        {
            token.ThrowIfCancellationRequested();

            if (retry)
                Logger?.LogTrace($"Retrying action for component '{component.GetUniqueId()}'");

            OnProgress(component, 0.0);
            var operationResult = action();

            switch (operationResult)
            {
                case InstallOperationResult.Success:
                    return InstallResult.Success;
                case InstallOperationResult.Canceled:
                    return InstallResult.Cancel;
                case InstallOperationResult.Failed:
                    return InstallResult.Failure;
                case InstallOperationResult.NoPermission:
                    return InstallResult.FailureElevationRequired;
                case InstallOperationResult.LockedFile:
                default:
                    var interactionResult = interaction(operationResult);
                    result = interactionResult.InstallResult;
                    retry = interactionResult.Retry;
                    break;
            }
        } while (retry);

        return result;
    }

    private void OnProgress(IInstallableComponent component, double progress)
    {
        Progress?.Invoke(this, new ComponentProgressEventArgs(progress, component.GetDisplayName()));
    }

    private void LogFailure(IProductComponent? component, InstallAction executeAction, string details)
    {
        Logger?.LogError(component != null
            ? $"Component '{component.GetDisplayName()}' failed to {executeAction.ToString().ToLowerInvariant()}. {details}"
            : $"Failed to {executeAction.ToString().ToLowerInvariant()}. {details}");
    }

    private protected enum InstallAction
    {
        Install,
        Remove
    }
}