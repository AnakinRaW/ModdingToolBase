using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase;

public abstract class CliBootstrapper : BootstrapperBase
{
    protected sealed override int Execute(string[] args, IServiceCollection serviceCollection)
    {
        // By default we update the application using the current branch
        var updaterOptions = CommandLineUpdaterOptions.Default;
        
        Parser.Default.ParseArguments<CommandLineUpdaterOptions>(args).WithParsed(options => updaterOptions = options);

        using var updateServiceProvider = serviceCollection.BuildServiceProvider();
        new CommandLineToolSelfUpdater(updaterOptions, updateServiceProvider).UpdateIfNecessary();

        return ExecuteAfterUpdate(args, serviceCollection);
    }

    protected abstract int ExecuteAfterUpdate(string[] args, IServiceCollection serviceCollection);
}