using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Packages.Rider.Editor.UnitTesting
{
  public class TestsCallback : ScriptableObject, ICallbacks
    {
        public void RunFinished(ITestResultAdaptor result)
        {
          CallbackData.instance.isRider = false;
          
          if (EditorApplication.isPlaying)
          {
            EditorApplication.playModeStateChanged += WaitForExitPlaymode;
            return;
          }
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.RunFinished, "", "","", 0, ResultState.Success.ToString(), ""));
          CallbackData.instance.RaiseChangedEvent();
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
          if (result.Method == null) return;
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.TestStarted, result.UniqueName, result.Method.TypeInfo.Assembly.GetName().Name, "", 0, "", result.ParentId));
          CallbackData.instance.RaiseChangedEvent();
        }

        public void TestFinished(ITestResultAdaptor result)
        {
          if (result.Test.Method == null) return;
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.TestFinished, result.Test.UniqueName, result.Test.Method.TypeInfo.Assembly.GetName().Name, result.Message, result.Duration, result.ResultState, result.Test.ParentId));
          CallbackData.instance.RaiseChangedEvent();
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }
    }
}