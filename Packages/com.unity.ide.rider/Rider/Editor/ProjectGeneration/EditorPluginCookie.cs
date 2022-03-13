using JetBrains.Annotations;
using UnityEditor;

namespace Packages.Rider.Editor.ProjectGeneration
{
  internal class EditorPluginCookie : ScriptableSingleton<EditorPluginCookie>
  {
    [UsedImplicitly] // from Rider UnityEditor plugin
    public bool syncIfNeededCalledFromRider;
  }
}
