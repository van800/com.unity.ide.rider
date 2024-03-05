using System;
using System.IO;
using System.Security;
using System.Text;
using Packages.Rider.Editor.Util;
using UnityEditor;

namespace Packages.Rider.Editor.ProjectGeneration {
  class FileIOProvider : IFileIO
  {
    public bool Exists(string path)
    {
      return File.Exists(path);
    }

    public TextReader GetReader(string path)
    {
      return new StreamReader(path);
    }

    public string ReadAllText(string path)
    {
      return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
      File.WriteAllText(path, content, Encoding.UTF8);
      LastWriteTracker.UpdateLastWriteIfNeeded(path);
    }

    internal static string GetPhysicalPath(string path)
    {
#if UNITY_2021_2_OR_NEWER
      var absolutePath = FileUtil.GetPhysicalPath(path);
#else
      var absolutePath = Path.GetFullPath(path);
#endif
      return absolutePath;
    }

    public string EscapedRelativePathFor(string file, string rootDirectoryFullPath)
    {
      // We have to normalize the path, because the PackageManagerRemapper assumes
      // dir seperators will be os specific.
      var absolutePath = GetPhysicalPath(file.NormalizePath());
      var path = SkipPathPrefix(absolutePath, rootDirectoryFullPath);
      return SecurityElement.Escape(path);
    }

    private static string SkipPathPrefix(string path, string prefix)
    {
      var root = prefix[prefix.Length - 1] == Path.DirectorySeparatorChar
        ? prefix
        : prefix + Path.DirectorySeparatorChar;
      return path.StartsWith(root, StringComparison.Ordinal)
        ? path.Substring(root.Length)
        : path;
    }
  }
}
