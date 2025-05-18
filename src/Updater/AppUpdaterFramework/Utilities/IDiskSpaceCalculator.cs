using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal interface IDiskSpaceCalculator
{
    void ThrowIfNotEnoughDiskSpaceAvailable(
        InstallableComponent newComponent, 
        InstallableComponent? oldComponent,
        string? installPath, 
        DiskSpaceCalculator.CalculationOptions options);
}