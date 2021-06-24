using System;
using JetBrains.Annotations;

namespace Packages.Rider.Editor.ProjectFiles
{
  [Serializable]
  [UsedImplicitly]
  internal class Data
  {
    public string path;
    public string content;

    public Data(string path, string content)
    {
      this.path = path;
      this.content = content;
    }
  }
}