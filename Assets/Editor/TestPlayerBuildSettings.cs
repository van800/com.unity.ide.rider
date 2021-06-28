

using AsmdefResponse.Tests;
using UnityEditor;
using UnityEditor.TestTools;

[assembly:TestPlayerBuildModifier(typeof(TestPlayerBuildSettings))]
/// <summary>
///     Assembly attribute above explained here: https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/reference-attribute-testplayerbuildmodifier.html
namespace AsmdefResponse.Tests
{
    public class TestPlayerBuildSettings : ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            playerOptions.options |= BuildOptions.AllowDebugging;
            return playerOptions;
        }
    }
}

