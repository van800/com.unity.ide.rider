using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor.ProjectFiles
{
  /// <summary>
  /// Is used by Rider Unity plugin by reflection
  /// </summary>
  [UsedImplicitly] // from Rider Unity plugin
  internal class ProjectFilesSyncData : ScriptableSingleton<ProjectFilesSyncData>
  {
    [UsedImplicitly] public static event EventHandler Changed = (sender, args) => { }; 

    internal void RaiseChangedEvent()
    {
      Changed(null, EventArgs.Empty);
    }
    
    [SerializeField] public List<Data> events = new List<Data>();

    /// <summary>
    /// Is used by Rider Unity plugin by reflection
    /// </summary>
    [UsedImplicitly]
    public void Clear()
    {
      events.Clear();
    }
  }
}