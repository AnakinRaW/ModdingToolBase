namespace AnakinRaW.AppUpdaterFramework.Security;

internal interface ISignatureVerifier
{
    /// <summary>
    /// Verifies the parsed signature end-to-end and returns the resulting <see cref="VerificationResult"/>.
    /// </summary>
    VerificationResult Verify(ParsedSignature parsed);
}
