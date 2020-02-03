using System;
using System.Collections.Generic;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.ProjectGeneration
{
  public interface IAssemblyNameProvider
  {
    string[] ProjectSupportedExtensions { get; }
    string ProjectGenerationRootNamespace { get; }
    string GetAssemblyNameFromScriptPath(string path);
    bool IsInternalizedPackagePath(string path);
    IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution);
    IEnumerable<string> GetAllAssetPaths();
    UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath);
    ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories);
    void GeneratePlayerProjects(bool generatePlayerProjects);
    IEnumerable<string> GetRoslynAnalyzerPaths();
  }
}