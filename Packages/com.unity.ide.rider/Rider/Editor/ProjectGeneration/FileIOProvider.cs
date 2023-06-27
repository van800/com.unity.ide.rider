using System;
using System.IO;
using System.Security;
using System.Text;
using Packages.Rider.Editor.Util;

namespace Packages.Rider.Editor.ProjectGeneration {
  class FileIOProvider : IFileIO
  {
    public bool Exists(string fileName)
    {
      return File.Exists(fileName);
    }

    public string ReadAllText(string fileName)
    {
      return File.ReadAllText(fileName);
    }

    public void WriteAllText(string path, string content)
    {
      File.WriteAllText(path, content, Encoding.UTF8);
      LastWriteTracker.UpdateLastWriteIfNeeded(path);
    }

    private static string EscapedRelativePathFor(string file, string projectDirectory)
    {
      var projectDir = Path.GetFullPath(projectDirectory);
      
      // We have to normalize the path, because the PackageManagerRemapper assumes
      // dir separators will be os specific.
      var absolutePath = Path.GetFullPath(file.NormalizePath());
      var path = SkipPathPrefix(absolutePath, projectDir);
      
      return SecurityElement.Escape(path);
    }

    private static string GetLink(string file)
    {
      file = file.NormalizePath();
      
      var prefix = $@"Packages{Path.DirectorySeparatorChar}".NormalizePath();
      if (file.StartsWith(prefix, StringComparison.Ordinal))
      {
        file = file.Substring(prefix.Length);
        var index = file.IndexOf(Path.DirectorySeparatorChar);
        if (index > 0)
          return file.Substring(index+1);
      }

      return null;
    }
    
    public string GetInclude(string includeType, string asset, string projectDirectory)
    {
      return GetIncludeInternal(includeType, asset, projectDirectory);
    }

    internal static string GetIncludeInternal(string includeType, string asset, string projectDirectory)
    {
      var fullFile = EscapedRelativePathFor(asset, projectDirectory);
      var link = GetLink(asset);
      return link != null
        ? $"     <{includeType} Include=\"{fullFile}\" Link=\"{link}\" />{Environment.NewLine}"
        : $"     <{includeType} Include=\"{fullFile}\" />{Environment.NewLine}";
    }

    private static string SkipPathPrefix(string path, string prefix)
    {
      return path.StartsWith($@"{prefix}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        ? path.Substring(prefix.Length + 1)
        : path;
    }
  }
}
