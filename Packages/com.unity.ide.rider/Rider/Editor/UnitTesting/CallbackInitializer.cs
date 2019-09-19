using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Packages.Rider.Tests.Editor.UnitTesting
{
  [InitializeOnLoad]
  internal static class CallbackInitializer
  {
    static CallbackInitializer()
    {
      if (CallbackData.instance.isRider)
        ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(ScriptableObject.CreateInstance<TestsCallback>(), 0);
    }
  }
}