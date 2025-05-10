using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Handlers;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.Installer;

internal class FileInstaller(IServiceProvider serviceProvider) : InstallerBase(serviceProvider)
{
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
    private readonly ILockedFileHandler _lockedFileHandler = serviceProvider.GetRequiredService<ILockedFileHandler>();

    protected override InstallResult InstallCore(
        IInstallableComponent component, 
        IFileInfo source,
        IReadOnlyDictionary<string, string> variables, 
        CancellationToken token)
    {
        if (source == null) 
            throw new ArgumentNullException(nameof(source));
        
        if (component is not SingleFileComponent singleFileComponent)
            throw new ArgumentException($"Component must be of type {nameof(SingleFileComponent)}");

        var filePath = singleFileComponent.GetFile(_fileSystem, variables);

        return ExecuteWithInteractiveRetry(component,
            () => CopyFile(filePath, source),
            interaction => HandlerInteraction(filePath, interaction),
            token);
    }

    protected override InstallResult RemoveCore(
        IInstallableComponent component,
        IReadOnlyDictionary<string, string> variables, 
        CancellationToken token)
    {
        if (component is not SingleFileComponent singleFileComponent)
            throw new NotSupportedException($"Component must be of type {nameof(SingleFileComponent)}");

        var filePath = singleFileComponent.GetFile(_fileSystem, variables);
        return ExecuteWithInteractiveRetry(component,
            () => DeleteFile(filePath),
            interaction => HandlerInteraction(filePath, interaction),
            token);
    }

    private InstallOperationResult CopyFile(IFileInfo destination, IFileInfo source)
    {
        if (!source.Exists)
            throw new FileNotFoundException($"Source file '{destination.FullName}' not found.");

        if (destination.Directory is null)
            throw new InvalidOperationException("destination directory must not be null");

        destination.Directory.Create();

        Stream? destinationStream = null;

        var fileCreateResult = DoFileAction(destination, InstallAction.Remove, file =>
        {
            destinationStream = _fileSystem.CreateFileWithRetry(destination.FullName);
            if (destinationStream is null)
            {
                Logger?.LogTrace($"Creation of file '{file.FullName}' failed.");
                return InstallOperationResult.Failed;
            }
            return InstallOperationResult.Success;
        });

        if (fileCreateResult != InstallOperationResult.Success)
            return fileCreateResult;

        if (destination is null)
            throw new InvalidOperationException("Destination stream must not be null!");

        using (destinationStream)
        {
            using var sourceStream = source.OpenRead();
            sourceStream.CopyTo(destinationStream!);
        }
        
        return InstallOperationResult.Success;
    }

    private InstallOperationResult DeleteFile(IFileInfo file)
    {
        if (!file.Exists)
        {
            Logger?.LogTrace($"'{file}' file is already deleted.");
            return InstallOperationResult.Success;
        }

        return DoFileAction(file, InstallAction.Remove, fileToDelete =>
        {
            var deleteSuccess = fileToDelete.TryDeleteWithRetry(2, 500, (ex, _) =>
            {
                Logger?.LogTrace(
                    $"Error occurred while deleting file '{fileToDelete}'. Error details: {ex.Message}. Retrying after {0.5f} seconds...");
                return true;
            });
            Logger?.LogTrace(deleteSuccess ? $"Source '{fileToDelete}' deleted." : $"Source '{fileToDelete}' was not deleted");
            return deleteSuccess ? InstallOperationResult.Success : InstallOperationResult.Failed;
        });
    }

    private InstallOperationResult DoFileAction(IFileInfo file, InstallAction action, Func<IFileInfo, InstallOperationResult> fileOperation)
    {
        try
        {
            return fileOperation(file);
        }
        catch (IOException e) when (new HRESULT(e.HResult).Code == Win32Error.ERROR_SHARING_VIOLATION)
        {
            Logger?.LogWarning($"Source '{file}' is locked");
            return InstallOperationResult.LockedFile;
        }
        catch (UnauthorizedAccessException)
        {
            Logger?.LogWarning($"Missing permission on Source '{file}'");
            return InstallOperationResult.NoPermission;
        }
        catch (Exception e)
        {
            Logger?.LogError(e, $"Unable to perform {action} on file '{file}': {e.Message}");
            return InstallOperationResult.Failed;
        }
    }

    private InstallerInteractionResult HandlerInteraction(IFileInfo file, InstallOperationResult operationResult)
    {
        return operationResult switch
        {
            InstallOperationResult.LockedFile => HandleLockedFile(file),
            InstallOperationResult.Success => new InstallerInteractionResult(InstallResult.Success),
            InstallOperationResult.Failed => new InstallerInteractionResult(InstallResult.Failure),
            InstallOperationResult.Canceled => new InstallerInteractionResult(InstallResult.Cancel),
            _ => throw new NotSupportedException($"OperationResult '{operationResult}' is not supported by this installer")
        };
    }

    private InstallerInteractionResult HandleLockedFile(IFileInfo file)
    { 
        try
        {
            var result = _lockedFileHandler.Handle(file);

            return result switch
            {
                // The file was unlocked --> We can try again
                ILockedFileHandler.Result.Unlocked => new InstallerInteractionResult(InstallResult.Failure, true),

                // The file is still locked --> We cannot proceed.
                ILockedFileHandler.Result.Locked => new InstallerInteractionResult(InstallResult.Cancel),

                // The file is still locked but an application restart can solve the problem
                ILockedFileHandler.Result.RequiresRestart => new InstallerInteractionResult(InstallResult.SuccessRestartRequired),

                _ => throw new ArgumentOutOfRangeException()
            };
        }
        catch (Exception e)
        {
            Logger?.LogWarning(e, e.Message);
            return new InstallerInteractionResult(InstallResult.Failure);
        }
    }
}