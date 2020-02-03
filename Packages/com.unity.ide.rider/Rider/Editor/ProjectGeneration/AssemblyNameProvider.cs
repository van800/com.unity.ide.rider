using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal class AssemblyNameProvider : IAssemblyNameProvider
  {
    bool m_generatePlayerProjects;

    public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationUserExtensions;

    public string ProjectGenerationRootNamespace => EditorSettings.projectGenerationRootNamespace;

    public string GetAssemblyNameFromScriptPath(string path)
    {
      return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
    }

    public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
    {
      foreach (var assembly in CompilationPipeline.GetAssemblies())
      {
        if (0 < assembly.sourceFiles.Length && assembly.sourceFiles.Any(shouldFileBePartOfSolution))
        {
          yield return assembly;
        }
      }
      if (m_generatePlayerProjects)
      {
        foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
        {
          if (0 < assembly.sourceFiles.Length && assembly.sourceFiles.Any(shouldFileBePartOfSolution))
          {
            yield return new Assembly(assembly.name + "-player", assembly.outputPath, assembly.sourceFiles, assembly.defines, assembly.assemblyReferences, assembly.compiledAssemblyReferences, assembly.flags)
            {
              compilerOptions =
              {
                ResponseFiles = assembly.compilerOptions.ResponseFiles,
                AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
                ApiCompatibilityLevel = assembly.compilerOptions.ApiCompatibilityLevel
              }
            };
          }
        }
      }
    }

    public IEnumerable<string> GetAllAssetPaths()
    {
      return AssetDatabase.GetAllAssetPaths();
    }

    public UnityEditor.PackageManager.PackageInfo FindForAssetPath(string assetPath)
    {
      return UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
    }

    public bool IsInternalizedPackagePath(string path)
    {
      if (string.IsNullOrEmpty(path.Trim()))
      {
        return false;
      }
      var packageInfo = FindForAssetPath(path);
      if (packageInfo == null)
      {
        return false;
      }
      var packageSource = packageInfo.source;
      return packageSource != PackageSource.Embedded && packageSource != PackageSource.Local;
    }

    public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
    {
      return CompilationPipeline.ParseResponseFile(
        responseFilePath,
        projectDirectory,
        systemReferenceDirectories
      );
    }

    public void GeneratePlayerProjects(bool generatePlayerProjects)
    {
      m_generatePlayerProjects = generatePlayerProjects;
    }

    public IEnumerable<string> GetRoslynAnalyzerPaths()
    {
      return PluginImporter.GetAllImporters()
        .Where(i => !i.isNativePlugin && AssetDatabase.GetLabels(i).SingleOrDefault(l => l == "RoslynAnalyzer") != null)
        .Select(i => i.assetPath);
    }
  }
}