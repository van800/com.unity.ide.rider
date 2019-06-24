using System.Collections.Generic;
using Editor;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools.TestRunner.GUI;

namespace Packages.Rider.Editor.UnitTesting
{
  internal class CallbackData : ScriptableSingleton<CallbackData>
  {
    [SerializeField]
    internal TestRunnerFilter runFilter;

    [SerializeField]
    internal TestMode testMode;
        
    public List<TestEvent> DelayedEvents = new List<TestEvent>();
  }
}