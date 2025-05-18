namespace AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;

public interface IDetectionCondition
{
    ConditionType Type { get; }

    string Id { get; }
}