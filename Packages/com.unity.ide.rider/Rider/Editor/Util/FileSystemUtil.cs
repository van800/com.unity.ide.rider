using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace Packages.Rider.Editor.Util
{
  public static class FileSystemUtil
  {
    [NotNull]
    public static string GetFinalPathName([NotNull] string path)
    {
      if (path == null) throw new ArgumentNullException("path");

      // up to MAX_PATH. MAX_PATH on Linux currently 4096, on Mac OS X 1024
      // doc: http://man7.org/linux/man-pages/man3/realpath.3.html
      var sb = new StringBuilder(8192);
      var result = LibcNativeInterop.realpath(path, sb);
      if (result == IntPtr.Zero)
      {
        var exception = new Win32Exception($"{path} was not resolved.");
        throw exception;
      }

      return new FileInfo(sb.ToString()).FullName;
    }
  }
}