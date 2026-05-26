using CommandLine;

namespace AnakinRaW.FtpUploader;

[Verb("local", HelpText = "Upload to a local repository.")]
internal class LocalUploadOptions : UploadOptions
{
    public override bool IsLocal => true;
}