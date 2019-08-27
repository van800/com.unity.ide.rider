using System;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor
{
  public class RiderScriptEditorData : ScriptableSingleton<RiderScriptEditorData>
  {
    [SerializeField] internal bool HasChanges = true; // sln/csproj files were changed

    [SerializeField] internal string currentEditorVersion; 
    [SerializeField] internal bool shouldLoadEditorPlugin;

    public void Init()
    {
      if (string.IsNullOrEmpty(currentEditorVersion))
        Invalidate(RiderScriptEditor.CurrentEditor);
    }

    public void Invalidate(string editorInstallationPath)
    {
      currentEditorVersion = RiderPathLocator.GetBuildNumber(editorInstallationPath);
      if (!Version.TryParse(currentEditorVersion, out var version))
        shouldLoadEditorPlugin = false;

      shouldLoadEditorPlugin = version >= new Version("191.7141.156");
    }
  }
}