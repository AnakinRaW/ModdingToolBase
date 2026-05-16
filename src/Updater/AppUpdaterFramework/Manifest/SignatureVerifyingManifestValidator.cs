using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

internal sealed class SignatureVerifyingManifestValidator(ManifestVerifierBase verifier) : IDownloadValidator
{
    private readonly ManifestVerifierBase _verifier = verifier ?? throw new ArgumentNullException(nameof(verifier));

    public Task<bool> ValidateAsync(Stream stream, long downloadedBytes, CancellationToken token = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (stream.CanSeek)
            stream.Position = 0;

        var result = _verifier.Verify(stream);
        return result == VerificationResult.Ok 
            ? Task.FromResult(true) 
            : throw new SignatureVerificationFailedException(result);
    }
}
