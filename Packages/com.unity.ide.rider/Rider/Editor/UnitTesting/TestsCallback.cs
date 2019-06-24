using Editor;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using EventType = Editor.EventType;

namespace Packages.Rider.Editor.UnitTesting
{
  public class TestsCallback : ScriptableObject, ICallbacks
    {
        public void RunFinished(ITestResultAdaptor result)
        {
          if (EditorApplication.isPlaying)
          {
            EditorApplication.playModeStateChanged += WaitForExitPlaymode;
            return;
          }
          
          CallbackData.instance.DelayedEvents.Add(
            new TestEvent(EventType.RunFinished, "", "","", 0, ResultState.Success.ToString(), ""));
        }

        private static void WaitForExitPlaymode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
              int i = 0;
            }
        }

        public void TestStarted(ITestAdaptor result)
        {
//          if (!(result is TestMethod)) return;
if (result.Method == null) return;
          
          CallbackData.instance.DelayedEvents.Add(
            new TestEvent(EventType.TestStarted, result.UniqueName, result.Method.TypeInfo.Assembly.GetName().Name, "", 0, "", result.ParentId));
        }

        public void TestFinished(ITestResultAdaptor result)
        {
//          if (!(result.Test.Method is TestMethod)) return;
//          
          if (result.Test.Method == null) return;
          
          CallbackData.instance.DelayedEvents.Add(
            new TestEvent(EventType.TestFinished, result.Test.UniqueName, result.Test.Method.TypeInfo.Assembly.GetName().Name, result.Message, result.Duration, result.ResultState, result.Test.ParentId));
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }
    }
}