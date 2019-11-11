using System;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor
{
  public class RiderScriptEditorData : ScriptableSingleton<RiderScriptEditorData>
  {
    [SerializeField] internal bool hasChanges = true; // sln/csproj files were changed 
    [SerializeField] internal bool shouldLoadEditorPlugin;
    [SerializeField] internal bool initializedOnce;
    [SerializeField] internal string editorBuildNumber;
    [SerializeField] internal RiderPathLocator.ProductInfo productInfo;

    public void Init()
    {
      if (string.IsNullOrEmpty(editorBuildNumber))
      {
        Invalidate(RiderScriptEditor.CurrentEditor);
      }
    }

    public void Invalidate(string editorInstallationPath)
    {
      editorBuildNumber = RiderPathLocator.GetBuildNumber(editorInstallationPath);
      productInfo = RiderPathLocator.GetBuildVersion(editorInstallationPath);
      if (!Version.TryParse(editorBuildNumber, out var version))
        shouldLoadEditorPlugin = false;

      shouldLoadEditorPlugin = version >= new Version("191.7141.156");
    }
  }
}