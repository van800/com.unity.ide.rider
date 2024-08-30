using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.Assemblies;

namespace Packages.Rider.Editor
{
  internal static class EditorPluginInterop
  {
    private static string EditorPluginAssemblyNamePrefix = "JetBrains.Rider.Unity.Editor.Plugin.";
    public static readonly string EditorPluginAssemblyName = $"{EditorPluginAssemblyNamePrefix}CorCLR.Repacked";
    private static string ourEntryPointTypeName = "JetBrains.Rider.Unity.Editor.PluginEntryPoint";

    private static Assembly ourEditorPluginAssembly;

    public static Assembly EditorPluginAssembly
    {
      get
      {
        if (ourEditorPluginAssembly != null)
          return ourEditorPluginAssembly;
        var assemblies = CurrentAssemblies.GetLoadedAssemblies();
        ourEditorPluginAssembly = assemblies.FirstOrDefault(a =>
        {
          try
          {
            return a.GetName().Name.StartsWith(EditorPluginAssemblyNamePrefix); // some user assemblies may fail here
          }
          catch (Exception)
          {
            // ignored
          }

          return default;
        });
        return ourEditorPluginAssembly;
      }
    }
    
    public static string LogPath
    {
      get
      {
        try
        {
          var assembly = EditorPluginAssembly;
          if (assembly == null) return null;
          var type = assembly.GetType(ourEntryPointTypeName);
          if (type == null) return null;
          var field = type.GetField("LogPath", BindingFlags.NonPublic | BindingFlags.Static);
          if (field == null) return null;
          return field.GetValue(null) as string;
        }
        catch (Exception)
        {
          Debug.Log("Unable to do OpenFile to Rider from dll, fallback to com.unity.ide.rider implementation.");
        }

        return null;
      }
    }

    public static bool OpenFileDllImplementation(string path, int line, int column)
    {
      var openResult = false;
      // reflection for fast OpenFileLineCol, when Rider is started and protocol connection is established
      try
      {
        var assembly = EditorPluginAssembly;
        if (assembly == null) return false;
        var type = assembly.GetType(ourEntryPointTypeName);
        if (type == null) return false;
        var field = type.GetField("OpenAssetHandler", BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null) return false;
        var handlerInstance = field.GetValue(null);
        var method = handlerInstance.GetType()
          .GetMethod("OnOpenedAsset", new[] {typeof(string), typeof(int), typeof(int)});
        if (method == null) return false;
        var assetFilePath = path;
        if (!string.IsNullOrEmpty(path))
          assetFilePath = FileUtil.GetPhysicalPath(path);
        
        openResult = (bool) method.Invoke(handlerInstance, new object[] {assetFilePath, line, column});
      }
      catch (Exception e)
      {
        Debug.Log("Unable to do OpenFile to Rider from dll, fallback to com.unity.ide.rider implementation.");
        Debug.LogException(e);
      }

      return openResult;
    }
  }
}