using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Packages.Rider.Editor.Util;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Packages.Rider.Editor
{
  [InitializeOnLoad]
  public class RiderScriptEditor : IExternalCodeEditor
  {
    IDiscovery m_Discoverability;
    IGenerator m_ProjectGeneration;
    RiderInitializer m_Initiliazer = new RiderInitializer();

    static RiderScriptEditor()
    {
      try
      {
        var projectGeneration = new ProjectGeneration();
        var editor = new RiderScriptEditor(new Discovery(), projectGeneration);
        CodeEditor.Register(editor);
        
        var path = GetEditorRealPath(CodeEditor.CurrentEditorInstallation);
        if (IsRiderInstallation(path))
        {
          editor.CreateIfDoesntExist();
          if (ShouldLoadAssembly(path))
          {
            editor.m_Initiliazer.Initialize(path);
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogException(e);
      }
    }

    private static string GetEditorRealPath(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return path;
      }

      if (!new FileInfo(path).Exists)
      {
        return path;
      }
      
      if (SystemInfo.operatingSystemFamily != OperatingSystemFamily.Windows)
      {
        var realPath = FileSystemUtil.GetFinalPathName(path);
        
        // case of snap installation
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
        {
          if (new FileInfo(path).Name.ToLowerInvariant() == "rider" &&
              new FileInfo(realPath).Name.ToLowerInvariant() == "snap")
          {
            var snapInstallPath = "/snap/rider/current/bin/rider.sh";
            if (new FileInfo(snapInstallPath).Exists)
              return snapInstallPath;
          }
        }
        
        // in case of symlink
        return realPath;
      }

      return path;
    }

    const string unity_generate_all = "unity_generate_all_csproj";
    static bool IsOSX => Application.platform == RuntimePlatform.OSXEditor;

    public RiderScriptEditor(IDiscovery discovery, IGenerator projectGeneration)
    {
      m_Discoverability = discovery;
      m_ProjectGeneration = projectGeneration;
    }

    private static string[] defaultExtensions
    {
      get
      {
        var customExtensions = new[] {"json", "asmdef", "log"};
        return EditorSettings.projectGenerationBuiltinExtensions.Concat(EditorSettings.projectGenerationUserExtensions)
          .Concat(customExtensions).Distinct().ToArray();
      }
    }

    private static string[] HandledExtensions
    {
      get
      {
        return HandledExtensionsString.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('.', '*'))
          .ToArray();
      } 
    }

    private static string HandledExtensionsString
    {
      get { return EditorPrefs.GetString("Rider_UserExtensions", string.Join(";", defaultExtensions));}
      set { EditorPrefs.SetString("Rider_UserExtensions", value); }
    }
    
    private static bool SupportsExtension(string path)
    {
      var extension = Path.GetExtension(path);
      if (string.IsNullOrEmpty(extension))
        return false; 
      return HandledExtensions.Contains(extension.TrimStart('.'));
    }

    public void OnGUI()
    {
      var prevGenerate = EditorPrefs.GetBool(unity_generate_all, false);
      var generateAll = EditorGUILayout.Toggle("Generate all .csproj files.", prevGenerate);
      if (generateAll != prevGenerate)
      {
        EditorPrefs.SetBool(unity_generate_all, generateAll);
      }
      
      m_ProjectGeneration.GenerateAll(generateAll);
      
      HandledExtensionsString = EditorGUILayout.TextField(new GUIContent("Extensions handled: "), HandledExtensionsString);
    }

    public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles,
      string[] importedFiles)
    {
      m_ProjectGeneration.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles),
        importedFiles);
    }

    public void SyncAll()
    {
      AssetDatabase.Refresh(); // refresh would automatically call SyncIfNeeded for changed files
    }

    public void Initialize(string editorInstallationPath) // is called each time ExternalEditor is changed
    {
      m_ProjectGeneration.Sync(); // regenerate csproj and sln for new editor
    }

    public bool OpenProject(string path, int line, int column)
    {
      if (path != "" && !SupportsExtension(path)) // Assets - Open C# Project passes empty path here
      {
        return false;
      }

      var fastOpenResult = EditorPluginInterop.OpenFileDllImplementation(path, line, column);
      if (fastOpenResult)
        return true;

      if (IsOSX)
      {
        return OpenOSXApp(path, line, column);
      }

      var solution = GetSolutionFile(path); // TODO: If solution file doesn't exist resync.
      solution = solution == "" ? "" : $"\"{solution}\"";
      var process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = CodeEditor.CurrentEditorInstallation,
          Arguments = $"{solution} -l {line} \"{path}\"",
          UseShellExecute = true,
        }
      };

      process.Start();

      return true;
    }

    private bool OpenOSXApp(string path, int line, int column)
    {
      var solution = GetSolutionFile(path); // TODO: If solution file doesn't exist resync.
      solution = solution == "" ? "" : $"\"{solution}\"";
      var pathArguments = path == "" ? "" : $"-l {line} \"{path}\"";
      var process = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = "open",
          Arguments = $"\"{CodeEditor.CurrentEditorInstallation}\" --args {solution} {pathArguments}",
          CreateNoWindow = true,
          UseShellExecute = true,
        }
      };

      process.Start();

      return true;
    }

    private string GetSolutionFile(string path)
    {
      if (UnityEditor.Unsupported.IsDeveloperBuild())
      {
        var baseFolder = GetBaseUnityDeveloperFolder();
        var lowerPath = path.ToLowerInvariant();

        bool isUnitySourceCode = lowerPath.Contains((baseFolder + "/Runtime").ToLowerInvariant());

        if (lowerPath.Contains((baseFolder + "/Editor").ToLowerInvariant()))
        {
          isUnitySourceCode = true;
        }

        if (isUnitySourceCode)
        {
          return Path.Combine(baseFolder, "Projects/CSharp/Unity.CSharpProjects.gen.sln");
        }
      }

      var solutionFile = m_ProjectGeneration.SolutionFile();
      if (File.Exists(solutionFile))
      {
        return solutionFile;
      }

      return "";
    }

    static string GetBaseUnityDeveloperFolder()
    {
      return Directory.GetParent(EditorApplication.applicationPath).Parent.Parent.FullName;
    }

    public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
    {
      if (new FileInfo(editorPath).Exists && IsRiderInstallation(editorPath))
      {
        var info = new RiderPathLocator.RiderInfo(editorPath, false);
        installation = new CodeEditor.Installation
        {
          Name = info.Presentation,
          Path = info.Path
        };
        return true;
      }

      installation = default;
      return false;
    }

    public static bool IsRiderInstallation(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return false;
      }

      var fileInfo = new FileInfo(path);
      var filename = fileInfo.Name.ToLowerInvariant();
      return filename.StartsWith("rider", StringComparison.Ordinal);
    }

    static bool ShouldLoadAssembly(string path)
    {
      var ver = RiderPathLocator.GetBuildNumber(path);
      if (!Version.TryParse(ver, out var version))
        return false;

      return version >= new Version("191.7141.156");
    }

    public CodeEditor.Installation[] Installations => m_Discoverability.PathCallback();

    public void CreateIfDoesntExist()
    {
      if (!m_ProjectGeneration.HasSolutionBeenGenerated())
      {
        m_ProjectGeneration.Sync();
      }
    }
  }
}