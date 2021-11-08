using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Editor
{
    public static class TestLocalPackageProjectGenerator
    {
        //static readonly string k_TestFolderPath = Path.Combine(Path.GetTempPath(), "RiderPackageTestFolder");
        private static readonly string k_TestFolderPath = "TestLocalPackageProjectGenerator";
        
        [MenuItem("Tests/GenerateLocalPackages")]
        static void GenerateScriptsAndAsmDefs()
        {
            RestorePreviousState();
            
            const int assemblyCount = 100;

            var asmdefReferences = new List<string>();
            asmdefReferences.Capacity = assemblyCount;

            var dependencies = new List<string>();
            for (int i = 0; i < assemblyCount; ++i)
            {
                var packageName = $"test.generator.package.{i}";
                var dirPath = Path.Combine(k_TestFolderPath, packageName);
                var packageContent = $@"{{
    ""name"": ""{packageName}"",
    ""displayName"": ""{packageName}"",
    ""description"": ""{packageName}"",
    ""version"": ""1.0.0"",
    ""unity"": ""2020.1""
}}";
                var asmdefContents = $@"{{ ""name"" : ""{packageName}"" }}";
                asmdefReferences.Add(packageName);

                Directory.CreateDirectory(dirPath);
                File.WriteAllText(Path.Combine(dirPath, "package.json"), packageContent);
                File.WriteAllText(Path.Combine(dirPath, $"{packageName}.asmdef"), asmdefContents);
                for (int j = 0; j < 250; j++)
                {
                  File.WriteAllText(Path.Combine(dirPath, $"Script{i}{j}.cs"), $"public class Script{i}{j} {{ }}");
                  File.WriteAllText(Path.Combine(dirPath, $"Script{i}{j}.txt"), $"public class Script{i}{j} {{ }}");
                }

                dependencies.Add($"\"{packageName}\": \"file:../{k_TestFolderPath}/{packageName}\"");
            }
            
            if (!File.Exists("Packages/manifest.json.backup")) 
                File.Move("Packages/manifest.json", "Packages/manifest.json.backup");

            var manifestJsonContent = $@"
{{
  ""disableProjectUpdate"": true,
            ""dependencies"": {{
                {dependencies.Aggregate((a, b) => a + "," + Environment.NewLine + b)}
            }}
}}
    ";
            File.WriteAllText("Packages/manifest.json", manifestJsonContent);
        }

        [MenuItem("Tests/Delete GenerateLocalPackages")]
        static void RestorePreviousState()
        {
            if (Directory.Exists(k_TestFolderPath))
                Directory.Delete(k_TestFolderPath, true);

            if (File.Exists("Packages/manifest.json.backup"))
            {
                File.Delete("Packages/manifest.json");
                File.Move("Packages/manifest.json.backup", "Packages/manifest.json");
            }
        }
    }
}