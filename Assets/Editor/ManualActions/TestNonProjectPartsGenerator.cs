using System.Collections.Generic;
using System.IO;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Editor
{
    public static class TestNonProjectPartsGenerator
    {
        static readonly string k_TestFolderPath = "Assets/TestFolderNonProjectParts";
        
        [MenuItem("Tests/GenerateNonProjectParts")]
        static void GenerateScriptsAndAsmDefs()
        {
            DeleteGeneratedScriptsAndAsmDefs();

            Directory.CreateDirectory(k_TestFolderPath);

            const int count = 200;

            var asmdefReferences = new List<string>();
            asmdefReferences.Capacity = count;

            for (int i = 0; i < count; ++i)
            {
                var name = $"file{i}";
                var dirPath = Path.Combine(k_TestFolderPath, name);
                var fileContent = $@"{{ ""name"" : ""{name}"" }}";

                asmdefReferences.Add(name);

                Directory.CreateDirectory(dirPath);
                File.WriteAllText(Path.Combine(dirPath, $"{name}.txt"), fileContent);
                for (int j = 0; j < 50; j++)
                {
                  File.WriteAllText(Path.Combine(dirPath, $"Script{i}{j}.txt"), $"public class Script{i}{j} {{ }}");
                }
            }
        }

        [MenuItem("Tests/Delete TestNonProjectPartsGenerator.GeneratedScriptsAndAsmDefs")]
        static void DeleteGeneratedScriptsAndAsmDefs()
        {
            if (Directory.Exists(k_TestFolderPath))
            {
                Directory.Delete(k_TestFolderPath, true);
                if (File.Exists(k_TestFolderPath+".meta"))
                    File.Delete(k_TestFolderPath+".meta");
            }
        }
        
    }
}