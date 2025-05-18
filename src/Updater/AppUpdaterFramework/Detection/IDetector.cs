using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;

namespace AnakinRaW.AppUpdaterFramework.Detection;

public interface IDetector
{
    bool Detect(IDetectionCondition condition, IReadOnlyDictionary<string, string> variables);
}