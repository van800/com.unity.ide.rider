using System.Text.RegularExpressions;
using System.Text.Json;
using static System.String;

namespace ReleaseTools;

public abstract class PrepareRelease
{
    private static string _releaseVersion = Empty;
    private static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Input a valid version of the package for the release");
            return;
        }

        _releaseVersion = args[0];

        ValidateVersionInput();
        CompareVersionInPackageJson();
        CheckIfChangelogIsCorrect();
    }

    private static void ValidateVersionInput()
    {
        const string validateVersionPattern = @"^\d+\.\d+\.\d+$";

        if (!Regex.IsMatch(_releaseVersion, validateVersionPattern))
        {
            throw new Exception($"Version {validateVersionPattern} is in the wrong format");
        }
    }

    private static void CompareVersionInPackageJson()
    {
        string[] jsonPaths =
            ["../Packages/com.unity.ide.rider/package.json", "../Packages/com.unity.ide.rider.tests/package.json"];
        foreach (var jsonPath in jsonPaths)
        {
            var jsonPackageFile = File.ReadAllText(jsonPath);
            var jsonPackageDocument = JsonDocument.Parse(jsonPackageFile);
            if (!jsonPackageDocument.RootElement.TryGetProperty("version", out var versionElement)) return;
            var jsonVersion = versionElement.GetString();
            if (jsonVersion != null && jsonVersion.Equals(_releaseVersion))
            {
                Console.WriteLine($"The version in the {jsonPath} file matches the expected version: {_releaseVersion}");
            }
            else
            {
                throw new Exception(
                    $"The version in the {jsonPath} file does not match the expected version: {_releaseVersion}");
            }
        }
    }

    private static void CheckIfChangelogIsCorrect()
    {
        const string changeLogPath = "../Packages/com.unity.ide.rider/CHANGELOG.md";
        var pattern = $@"^## \[{Regex.Escape(_releaseVersion)}\] -";

        if (IsNullOrEmpty(changeLogPath))
        {
            throw new ArgumentNullException(nameof(changeLogPath), "File path cannot be null or empty.");
        }

        var markdownContent = File.ReadAllText(changeLogPath);

        var match = Regex.Match(markdownContent, pattern, RegexOptions.Multiline);

        if (!match.Success)
        {
            throw new Exception($"Changlog should have an entry with {_releaseVersion}");
        }
    }
}
