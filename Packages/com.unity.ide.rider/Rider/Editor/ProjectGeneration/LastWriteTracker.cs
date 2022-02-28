using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Packages.Rider.Editor.ProjectGeneration
{
    internal static class LastWriteTracker
    {
        internal static bool HasLastWriteTimeChanged()
        {
            // any external changes of sln/csproj or manifest.json should cause their regeneration
            // Directory.GetCurrentDirectory(), "*.csproj", "*.sln", "Packages/manifest.json"
            var files = new List<FileInfo>();
            
            files.AddRange(new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("*.csproj"));
            files.AddRange(new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("*.sln"));

            return files.Any(a => a.LastWriteTime > RiderScriptEditorPersistedState.instance.LastWrite);
        }
        
        internal static void UpdateLastWriteIfNeeded(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Directory == null)
                return;
            
            if (fileInfo.Directory.FullName.Equals(new DirectoryInfo(Directory.GetCurrentDirectory()).FullName, StringComparison.OrdinalIgnoreCase) &&
                (fileInfo.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                 || fileInfo.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)))
            {
                RiderScriptEditorPersistedState.instance.LastWrite = fileInfo.LastWriteTime;
            }
        }
    }
}