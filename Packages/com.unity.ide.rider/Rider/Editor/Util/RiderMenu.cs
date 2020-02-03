using JetBrains.Annotations;
using Packages.Rider.Editor;
using Packages.Rider.Editor.Util;
using Unity.CodeEditor;
using UnityEditor;

// ReSharper disable once CheckNamespace 
namespace JetBrains.Rider.Unity.Editor
{
  [UsedImplicitly] // Is called via commandline from Rider Notification after checking out from source control.
  public static class RiderMenu
  {
    [UsedImplicitly]
    public static void MenuOpenProject()
    {
      if (RiderScriptEditor.IsRiderInstallation(RiderScriptEditor.CurrentEditor))
      {
        // Force the project files to be sync
        CodeEditor.CurrentEditor.SyncAll();

        // Load Project
        CodeEditor.CurrentEditor.OpenProject();
      }
    }
    
    /// <summary>
    /// Forces regeneration of .csproj / .sln files.
    /// </summary>
    [MenuItem("Assets/Sync C# Project", false, 1001)]
    private static void MenuSyncProject()
    {
      CodeEditor.CurrentEditor.SyncAll();
    }
    
    [MenuItem("Assets/Sync C# Project", true, 1001)]
    private static bool ValidateMenuSyncProject()
    {
      return true;
    }
  }
}