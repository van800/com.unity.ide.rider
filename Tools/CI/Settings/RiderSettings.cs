using RecipeEngine.Api.Artifacts;
using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Dependencies;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;

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

    public RiderSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );      
    }

    public WrenchSettings Wrench { get; private set; }
}
