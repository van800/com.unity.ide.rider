using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace Rider.Cookbook.Settings;

public class RiderSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = {"Packages/"};

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.ide.rider",
            new PackageOptions() { ReleaseOptions = new ReleaseOptions() { IsReleasing = true } }
        }
    };

    // You can either use a platform.json file or specify custom yamato VM images for each package in code.
    private readonly Dictionary<SystemType, Platform> ImageOverrides = new()
    {
        {
            SystemType.Ubuntu,
            new Platform(new Agent("package-ci/ubuntu-20.04:default", FlavorType.BuildLarge, ResourceType.Vm),
                SystemType.Ubuntu)
        }
    };

    public RiderSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );      
    }

    public WrenchSettings Wrench { get; private set; }
}
