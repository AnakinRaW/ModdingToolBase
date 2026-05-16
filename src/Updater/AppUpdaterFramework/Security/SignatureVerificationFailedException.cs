using System;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Thrown when a downloaded manifest fails signature verification. Signals a permanent,
/// non-retryable failure: the bytes on the wire are not trusted, so retrying the same source
/// will not change the outcome.
/// </summary>
public sealed class SignatureVerificationFailedException : Exception
{
    /// <summary>
    /// Gets the verification result that caused the failure.
    /// </summary>
    public VerificationResult Result { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureVerificationFailedException"/> class
    /// with the specified verification result.
    /// </summary>
    public SignatureVerificationFailedException(VerificationResult result)
        : base($"Manifest signature verification failed: {result}.")
    {
        Result = result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureVerificationFailedException"/> class
    /// with the specified verification result and a custom message.
    /// </summary>
    public SignatureVerificationFailedException(VerificationResult result, string message)
        : base(message)
    {
        Result = result;
    }
}
