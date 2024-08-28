using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Rider.Editor.Util;
using UnityEngine;
using UnityEngine.Assemblies;
using Debug = UnityEngine.Debug;

namespace Packages.Rider.Editor
{
    internal class RiderInitializer
    {
      [Unity.Scripting.LifecycleManagement.BeforeAssemblyUnloading] 
      [UsedImplicitly]
      static void CleanupResources()
      {
        CancellationTokenSource.Cancel();
      }

      private static readonly CancellationTokenSource CancellationTokenSource = new();
      private static readonly CancellationToken Token = CancellationTokenSource.Token;
      
      public void Initialize(string editorPath)
      {
        var assembly = EditorPluginInterop.EditorPluginAssembly;
        if (assembly != null) // already loaded RIDER-92419
        {
          return;
        }
        
        // for debugging rider editor plugin
        if (RiderPathUtil.IsRiderDevEditor(editorPath))
        {
          LoadEditorPluginForDevEditor(editorPath);
        }
        else
        {
          var relPath = "../../plugins/rider-unity/EditorPlugin";
          if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            relPath = "Contents/plugins/rider-unity/EditorPlugin";
          var baseDir = Path.Combine(editorPath, relPath);
          
          // prepare for the future,
          // when such assembly would appear in the Rider installation, it would "just work" with older Rider package
          var dllFile = new FileInfo(Path.Combine(baseDir, $"{EditorPluginInterop.EditorPluginAssemblyName}.dll"));
          if (dllFile.Exists)
          {
            var bytes = File.ReadAllBytes(dllFile.FullName);
            assembly = CurrentAssemblies.LoadFromBytes(bytes); // doesn't lock assembly on disk
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.TRACE)
              Debug.Log($"Rider EditorPlugin loaded from {dllFile.FullName}");
          
            InitEntryPoint(Token, assembly);
          }
          else
          {
            Debug.Log($"Unable to find Rider EditorPlugin {dllFile.FullName} for Unity ");
          }
        }
      }

      private static void LoadEditorPluginForDevEditor(string editorPath)
      {
        var file = new FileInfo(editorPath);
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
          file = new FileInfo(Path.Combine(editorPath, "rider-dev.bat"));
        
        if (!file.Exists)
        {
          Debug.Log($"Unable to determine path to EditorPlugin from {file}");
          return;
        }
        
        var dllPath = File.ReadLines(file.FullName).FirstOrDefault();

        if (dllPath == null)
        {
          Debug.Log($"Unable to determine path to EditorPlugin from {file}");
          return;
        }

        var dllFile = new FileInfo(dllPath);

        if (!dllFile.Exists)
        {
          Debug.Log($"Unable to find Rider EditorPlugin {dllPath} for Unity ");
          return;
        }

        var assembly = CurrentAssemblies.LoadFromPath(dllFile.FullName);
        if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.TRACE)
          Debug.Log($"Rider EditorPlugin loaded from {dllFile.FullName}");

        InitEntryPoint(Token, assembly);
      }

      private static void InitEntryPoint(CancellationToken token, Assembly assembly)
      {
        try
        {
          var type = assembly.GetType("JetBrains.Rider.Unity.Editor.PluginEntryPoint");
          var method = type.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Static);
          if (method == null) Debug.LogError($"Initialize method of {type} was not found.");
          object[] parameters = { token };
          method?.Invoke(null, parameters);
        }
        catch (TypeInitializationException ex)
        {
          Debug.LogException(ex);
          if (ex.InnerException != null) 
            Debug.LogException(ex.InnerException);
        }
        catch (Exception ex)
        {
          Debug.LogException(ex);
        }
      }
    }
}
