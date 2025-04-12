namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public readonly record struct InstallationSize(long SystemDrive, long ProductDrive)
{
    public long Total => SystemDrive + ProductDrive;
}