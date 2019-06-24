using Editor;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Packages.Rider.Editor.UnitTesting
{
  [InitializeOnLoad]
  internal static class CallbackInitializer
  {
    static CallbackInitializer()
    {
      ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(ScriptableObject.CreateInstance<TestsCallback>(), 0);
    }
  }
}