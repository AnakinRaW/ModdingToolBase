using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Product.Detectors;

internal abstract class ComponentDetectorBase<T> : IComponentDetector where T : IInstallableComponent
{
    protected IServiceProvider ServiceProvider { get; }

    protected ILogger? Logger { get; }

    protected ComponentDetectorBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }
        
    public bool GetCurrentInstalledState(IInstallableComponent installableComponent, ProductVariables productVariables)
    {
        if (installableComponent == null) 
            throw new ArgumentNullException(nameof(installableComponent));
        if (productVariables == null)
            throw new ArgumentNullException(nameof(productVariables));
        var component = ValidateSupported(installableComponent);
        return FindCore(component, productVariables);
    }

    protected abstract bool FindCore(T component, ProductVariables product);

    protected T ValidateSupported(IInstallableComponent component)
    {
        if (component is not T obj)
            throw new InvalidOperationException($"Component {component.GetType().Name} is of wrong type. " +
                                                $"Expected {nameof(SingleFileComponent)}");
        return obj;
    }

    public override string ToString()
    {
        return $"Detector: {GetType().Name}";
    }
}