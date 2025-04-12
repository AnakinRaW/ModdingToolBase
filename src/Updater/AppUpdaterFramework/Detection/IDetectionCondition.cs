namespace AnakinRaW.AppUpdaterFramework.Detection;

public interface IDetectionCondition
{
    ConditionType Type { get; }

    string Id { get; }
}