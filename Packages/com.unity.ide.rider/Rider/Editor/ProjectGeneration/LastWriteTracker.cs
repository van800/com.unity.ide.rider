using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal static class LastWriteTracker
  {
    internal static bool HasLastWriteTimeChanged()
    {
#if !UNITY_2020_1_OR_NEWER
      return false;
#else
      // any external changes of sln/csproj or manifest.json should cause their regeneration
      // Directory.GetCurrentDirectory(), "*.csproj", "*.sln"
      var files = new List<FileInfo>();

      var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
      files.AddRange(directoryInfo.GetFiles("*.csproj"));
      files.Add(new FileInfo(Path.Combine(directoryInfo.FullName, directoryInfo.Name + ".sln")));

      return files.Any(a => a.LastWriteTime > RiderScriptEditorPersistedState.instance.LastWrite);
#endif
    }

    internal static void UpdateLastWriteIfNeeded(string path)
    {
#if !UNITY_2020_1_OR_NEWER
      return;
#else
      var fileInfo = new FileInfo(path);
      if (fileInfo.Directory == null)
        return;
      var directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
      if (fileInfo.Directory.FullName.Equals(directoryInfo.FullName, StringComparison.OrdinalIgnoreCase) &&
          (fileInfo.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
           || fileInfo.Name.Equals(directoryInfo.Name + ".sln", StringComparison.OrdinalIgnoreCase)))
      {
        RiderScriptEditorPersistedState.instance.LastWrite = fileInfo.LastWriteTime;
      }
#endif
    }
  }
}