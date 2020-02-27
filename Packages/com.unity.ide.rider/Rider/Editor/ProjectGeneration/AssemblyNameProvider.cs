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
    ProjectGenerationFlag m_ProjectGenerationFlag = (ProjectGenerationFlag)EditorPrefs.GetInt("unity_project_generation_flag", 3);

    public string[] ProjectSupportedExtensions => EditorSettings.projectGenerationUserExtensions;

    public string ProjectGenerationRootNamespace => EditorSettings.projectGenerationRootNamespace;

    public ProjectGenerationFlag ProjectGenerationFlag
    {
      get => m_ProjectGenerationFlag;
      private set
      {
        EditorPrefs.SetInt("unity_project_generation_flag", (int)value);
        m_ProjectGenerationFlag = value;
      }
    }

    public string GetAssemblyNameFromScriptPath(string path)
    {
      return CompilationPipeline.GetAssemblyNameFromScriptPath(path);
    }

    public IEnumerable<Assembly> GetAssemblies(Func<string, bool> shouldFileBePartOfSolution)
    {
      foreach (var assembly in CompilationPipeline.GetAssemblies())
      {
        if (assembly.sourceFiles.Any(shouldFileBePartOfSolution))
        {
          yield return new Assembly(assembly.name, assembly.outputPath, assembly.sourceFiles, new[] { "DEBUG", "TRACE" }.Concat(assembly.defines).Concat(EditorUserBuildSettings.activeScriptCompilationDefines).ToArray(), assembly.assemblyReferences, assembly.compiledAssemblyReferences, assembly.flags)
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

      if (ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.PlayerAssemblies))
      {
        foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player).Where(assembly => assembly.sourceFiles.Any(shouldFileBePartOfSolution)))
        {
          yield return new Assembly(assembly.name + ".Player", assembly.outputPath, assembly.sourceFiles, new[] { "DEBUG", "TRACE" }.Concat(assembly.defines).ToArray(), assembly.assemblyReferences, assembly.compiledAssemblyReferences, assembly.flags)
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

    public string GetCompileOutputPath(string assemblyName)
    {
      return assemblyName.EndsWith(".Player", StringComparison.Ordinal) ? @"Temp\Bin\Debug\Player\" : @"Temp\Bin\Debug\";
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
      switch (packageSource)
      {
        case PackageSource.Embedded:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Embedded);
        case PackageSource.Registry:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Registry);
        case PackageSource.BuiltIn:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.BuiltIn);
        case PackageSource.Unknown:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Unknown);
        case PackageSource.Local:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Local);
        case PackageSource.Git:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.Git);
#if UNITY_2019_3_OR_NEWER
        case PackageSource.LocalTarball:
          return !ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.LocalTarBall);
#endif
      }

      return false;
    }

    public ResponseFileData ParseResponseFile(string responseFilePath, string projectDirectory, string[] systemReferenceDirectories)
    {
      return CompilationPipeline.ParseResponseFile(
        responseFilePath,
        projectDirectory,
        systemReferenceDirectories
      );
    }

    public IEnumerable<string> GetRoslynAnalyzerPaths()
    {
      return PluginImporter.GetAllImporters()
        .Where(i => !i.isNativePlugin && AssetDatabase.GetLabels(i).SingleOrDefault(l => l == "RoslynAnalyzer") != null)
        .Select(i => i.assetPath);
    }

    public void ToggleProjectGeneration(ProjectGenerationFlag preference)
    {
      if (ProjectGenerationFlag.HasFlag(preference))
      {
        ProjectGenerationFlag ^= preference;
      }
      else
      {
        ProjectGenerationFlag |= preference;
      }
    }

    public void ResetProjectGenerationFlag()
    {
      ProjectGenerationFlag = ProjectGenerationFlag.None;
    }
  }
}
