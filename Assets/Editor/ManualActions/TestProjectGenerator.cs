using System.Collections.Generic;
using System.IO;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Editor
{
    public static class TestProjectGenerator
    {
        static readonly string k_TestFolderPath = "Assets/TestFolder";
        
        [MenuItem("Tests/GenerateScriptsAndAsmDefs")]
        static void GenerateScriptsAndAsmDefs()
        {
            DeleteGeneratedScriptsAndAsmDefs();

            Directory.CreateDirectory(k_TestFolderPath);

            const int assemblyCount = 70;

            var asmdefReferences = new List<string>();
            asmdefReferences.Capacity = assemblyCount;

            for (int i = 0; i < assemblyCount; ++i)
            {
                var assemblyName = $"Assembly{i}";
                var dirPath = Path.Combine(k_TestFolderPath, assemblyName);
                var asmdefContents = $@"{{ ""name"" : ""{assemblyName}"" }}";

                asmdefReferences.Add(assemblyName);

                Directory.CreateDirectory(dirPath);
                File.WriteAllText(Path.Combine(dirPath, $"{assemblyName}.asmdef"), asmdefContents);
                for (int j = 0; j < 50; j++)
                {
                  File.WriteAllText(Path.Combine(dirPath, $"Script{i}{j}.cs"), $"public class Script{i}{j} {{ }}");
                  File.WriteAllText(Path.Combine(dirPath, $"Script{i}{j}.txt"), $"public class Script{i}{j} {{ }}");
                }
            }
        }

        [MenuItem("Tests/Delete GeneratedScriptsAndAsmDefs")]
        static void DeleteGeneratedScriptsAndAsmDefs()
        {
            if (Directory.Exists(k_TestFolderPath))
            {
                Directory.Delete(k_TestFolderPath, true);
            }
        }
        
    }
}