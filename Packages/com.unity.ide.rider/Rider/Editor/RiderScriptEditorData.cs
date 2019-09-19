using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Tests.Editor
{
  public class RiderScriptEditorData : ScriptableSingleton<RiderScriptEditorData>
  {
    [SerializeField] internal bool HasChanges = true; // sln/csproj files were changed
    
    [SerializeField] internal bool InitializedOnce;
  }
}