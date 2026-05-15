using AnakinRaW.AppUpdaterFramework.Json;
using ComponentType = AnakinRaW.AppUpdaterFramework.Metadata.Component.ComponentType;

namespace AnakinRaW.ApplicationManifestCreator.Test;

internal static class ManifestFactory
{
    public static ApplicationManifest CreateSample()
    {
        var components = new[]
        {
            new AppComponent(
                Id: "foo",
                Version: "1.2.3",
                Name: "Foo Component",
                Type: ComponentType.File,
                Items: null,
                OriginInfo: new OriginInfo("https://example.test/foo.dll", 1234, "deadbeef"),
                InstallPath: "$(InstallDir)",
                FileName: "foo.dll",
                InstallSize: new InstallSize(1024, 0),
                DetectConditions: null),
        };
        return new ApplicationManifest("TestApp", "1.0.0", "stable", components);
    }
}
