using System;
using System.Linq;
using Packages.Rider.Editor.EditorPlugin;
using Packages.Rider.Editor.Util;
using Unity.CodeEditor;

namespace Packages.Rider.Editor
{
  internal interface IDiscovery
  {
    CodeEditor.Installation[] PathCallback();
  }

  internal class Discovery : IDiscovery
  {
    public static readonly RiderPathLocator RiderPathLocator;

    static Discovery()
    {
      RiderPathLocator = new RiderPathLocator(new RiderLocatorEnvironment());
    }

    public CodeEditor.Installation[] PathCallback()
    {
      var res = RiderPathLocator.GetAllRiderPaths()
        .Select(riderInfo => new CodeEditor.Installation
        {
          Path = riderInfo.Path,
          Name = riderInfo.Presentation
        })
        .ToList();

      var editorPath = RiderScriptEditor.CurrentEditor;
      if (RiderScriptEditor.IsRiderOrFleetInstallation(editorPath) &&
          !res.Any(a => a.Path == editorPath) &&
          FileSystemUtil.EditorPathExists(editorPath))
      {
        // External editor manually set from custom location
        var info = new RiderPathLocator.RiderInfo(RiderPathLocator, editorPath, false);
        var installation = new CodeEditor.Installation
        {
          Path = info.Path,
          Name = info.Presentation
        };
        res.Add(installation);
      }

      return res.ToArray();
    }
  }

  internal class RiderLocatorEnvironment : IRiderLocatorEnvironment
  {
    public OperatingSystemFamily operatingSystemFamily
    {
      get
      {
        switch (UnityEngine.SystemInfo.operatingSystemFamily)
        {
          case UnityEngine.OperatingSystemFamily.Windows:
            return OperatingSystemFamily.Windows;
          case UnityEngine.OperatingSystemFamily.MacOSX:
            return OperatingSystemFamily.MacOSX;
          case UnityEngine.OperatingSystemFamily.Linux:
            return OperatingSystemFamily.Linux;
          default:
            return OperatingSystemFamily.Other;
        }
      }
    }

    public T FromJson<T>(string json)
    {
      return (T)UnityEngine.JsonUtility.FromJson(json, typeof(T));
    }

    public void Warn(string message, Exception e = null)
    {
      UnityEngine.Debug.LogError(message);
      if (e != null)
        UnityEngine.Debug.LogException(e);
    }

    public void LogException(Exception exception)
    {
      UnityEngine.Debug.LogException(exception);
    }
  }
}