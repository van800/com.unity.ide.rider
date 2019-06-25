using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Packages.Rider.Editor.UnitTesting
{
  public class CallbackData : ScriptableSingleton<CallbackData>
  {
    public bool isRider;

    [UsedImplicitly] public static event EventHandler Changed = (sender, args) => { }; 

    internal void RaiseChangedEvent()
    {
      Changed(null, EventArgs.Empty);
    }

    
    public DelayedEvents events;

    [UsedImplicitly]
    public string GetJsonAndClear()
    {
      var json = JsonUtility.ToJson(events);
      events.Clear();
      return json;
    }
    
    [Serializable]
    public class DelayedEvents
    {
      public List<TestEvent> events = new List<TestEvent>();

      public void Clear()
      {
        events.Clear();
      }
    }
  }
}