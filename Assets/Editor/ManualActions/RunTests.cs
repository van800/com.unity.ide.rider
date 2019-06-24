using Packages.Rider.Editor.UnitTesting;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Editor
{
    public static class RunTests
    {
        [MenuItem("Tests/Run tests in PlayMode")]
        static void RunTestsInPlayMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings()
            {
//                filter = new Filter()
//                {
//                    testMode = TestMode.PlayMode, // This can be used to change between editmode and playmode tests
//                }
                filters = new []{new Filter{testMode = TestMode.PlayMode}}
//                , targetPlatform =
//                    BuildTarget
//                        .StandaloneWindows64, // This can be used to set the target platform when running playmode tests.
            });

            api.RegisterCallbacks(ScriptableObject.CreateInstance<TestsCallback>()); // This can be used to receive information about when the test suite and individual tests starts and stops. Provide this with a scriptable object implementing ICallbacks
        }
    }
}