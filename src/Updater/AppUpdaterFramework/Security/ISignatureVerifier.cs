namespace AnakinRaW.AppUpdaterFramework.Security;

internal interface ISignatureVerifier
{ 
    VerificationResult Verify(ParsedSignature parsed);
}
