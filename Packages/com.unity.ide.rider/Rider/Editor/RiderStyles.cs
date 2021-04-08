using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor
{
  internal static class RiderStyles
  {
    public static GUIStyle LinkLabelStyle = new GUIStyle(EditorStyles.linkLabel) {padding = GUI.skin.label.padding};
  }
}