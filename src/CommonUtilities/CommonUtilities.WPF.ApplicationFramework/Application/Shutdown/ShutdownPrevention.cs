namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;

public sealed class ShutdownPrevention(string? reasonId)
{
    public string? ReasonId { get; } = reasonId;
}