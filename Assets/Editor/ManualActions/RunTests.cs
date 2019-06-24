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
            RiderTestRunner.RunTests(2, null, null, null, null); // 2 = PlayMode
        }
    }
}