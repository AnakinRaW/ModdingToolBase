using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Product.Detectors;

internal class ComponentDetectorFactory : IComponentDetectorFactory
{
    internal static readonly IComponentDetectorFactory Default = new ComponentDetectorFactory();

    private readonly Dictionary<ComponentType, IComponentDetector> _detectors = new();

    public IComponentDetector GetDetector(ComponentType type, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        return type switch
        {
            ComponentType.File => GetOrCreate(type, () => new DefaultComponentDetector(serviceProvider)),
            _ => throw new NotSupportedException()
        };
    }

    private IComponentDetector GetOrCreate(ComponentType type, Func<IComponentDetector> createFunc)
    {
        if (createFunc == null)
            throw new ArgumentNullException(nameof(createFunc));
        if (!_detectors.TryGetValue(type, out var detector))
        {
            detector = createFunc();
            _detectors[type] = detector ?? throw new InvalidOperationException("Detector must not be null!");
        }
        return detector;
    }
}