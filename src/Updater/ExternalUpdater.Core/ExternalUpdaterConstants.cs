namespace AnakinRaW.ExternalUpdater;

/// <summary>Provides constants and helpers that identify the external updater component.</summary>
public static class ExternalUpdaterConstants
{
    // NB: Changing this value is very dangerous,
    // as it is used to identify the component to the application updater framework.
    // If this value is changed, the updater framework might install the updated component,
    // while removing the old one (which is still using the same installed file name).
    // This would result in the updater being removed from the system entirely.
    /// <summary>The identity of the external updater component as registered with the application updater framework.</summary>
    public const string ComponentIdentity = "AnakinRaW.ExternalUpdater";

    /// <summary>Gets the file name of the external updater executable, including its extension.</summary>
    /// <returns>The file name of the external updater executable.</returns>
    public static string GetExecutableFileName()
    {
        return $"{ComponentIdentity}.exe";
    }
}