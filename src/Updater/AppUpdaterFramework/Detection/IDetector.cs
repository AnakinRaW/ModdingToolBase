using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Detection;

public interface IDetector
{
    bool Detect(IDetectionCondition condition, IReadOnlyDictionary<string, string> variables);
}