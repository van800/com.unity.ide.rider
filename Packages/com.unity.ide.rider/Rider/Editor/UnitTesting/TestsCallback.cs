using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Packages.Rider.Editor.UnitTesting
{
  public class TestsCallback : ScriptableObject, ICallbacks
    {
        public void RunFinished(ITestResultAdaptor result)
        {
          CallbackData.instance.isRider = false;
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.RunFinished, "", "","", 0, ResultState.Success.ToString(), ""));
          CallbackData.instance.RaiseChangedEvent();
        }

        public void TestStarted(ITestAdaptor result)
        {
          if (result.Method == null) return;
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.TestStarted, GetUniqueName(result), result.Method.TypeInfo.Assembly.GetName().Name, "", 0, "", result.ParentId));
          CallbackData.instance.RaiseChangedEvent();
        }

        public void TestFinished(ITestResultAdaptor result)
        {
          if (result.Test.Method == null) return;
          
          CallbackData.instance.events.events.Add(
            new TestEvent(EventType.TestFinished, GetUniqueName(result.Test), result.Test.Method.TypeInfo.Assembly.GetName().Name, result.Message, result.Duration, result.ResultState, result.Test.ParentId));
          CallbackData.instance.RaiseChangedEvent();
        }
        
        // todo: reimplement JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting.TestEventsSender.GetUniqueName
        private static string GetUniqueName(ITestAdaptor test)
        {
          string str = test.FullName;
          return str;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }
    }
}