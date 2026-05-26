namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Controls how strictly the framework enforces signature verification on downloaded manifests.
/// </summary>
public enum SignaturePolicy
{
    /// <summary>
    /// Manifests must be signed and must verify successfully. Any verification result other than
    /// <see cref="VerificationResult.Ok"/> aborts the update.
    /// </summary>
    Required = 0,

    /// <summary>
    /// Signature verification is skipped entirely.
    /// </summary>
    Off = 1,
}
