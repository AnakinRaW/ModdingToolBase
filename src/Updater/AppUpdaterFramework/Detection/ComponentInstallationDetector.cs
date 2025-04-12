using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Detection;

internal class ComponentInstallationDetector(IServiceProvider services) : IComponentInstallationDetector
{
    private readonly Dictionary<ConditionType, IDetector> _detectors = new();

    public bool IsInstalled(IInstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        if (variables == null)
            throw new ArgumentNullException(nameof(variables));

        foreach (var condition in component.DetectConditions)
        {
            var detector = GetDetector(condition);
            if (detector is null)
                throw new Exception($"Cannot find evaluator for {condition.Id} of type {condition.Type}");

            if (!detector.Detect(condition, variables))
                return false;
        }

        return true;
    }

    public IDetector GetDetector(IDetectionCondition condition)
    {
        if (_detectors.TryGetValue(condition.Type, out var detector))
            return detector;

        detector = condition.Type switch
        {
            ConditionType.File => new SingleFileDetector(services),
            _ => throw new NotSupportedException()
        };

        _detectors[condition.Type] = detector;

        return detector;
    }
}