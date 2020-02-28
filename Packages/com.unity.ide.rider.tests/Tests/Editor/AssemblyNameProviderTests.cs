using System.Linq;
using NUnit.Framework;
using Packages.Rider.Editor.ProjectGeneration;
using UnityEditor;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
{
    public class AssemblyNameProviderTests
    {
        AssemblyNameProvider m_AssemblyNameProvider;
        ProjectGenerationFlag m_Flag;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_AssemblyNameProvider = new AssemblyNameProvider();
            m_Flag = m_AssemblyNameProvider.ProjectGenerationFlag;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.None);
            m_AssemblyNameProvider.ToggleProjectGeneration(m_Flag);
        }

        [SetUp]
        public void SetUp()
        {
            m_AssemblyNameProvider.ResetProjectGenerationFlag();
        }

        [TestCase(@"Temp\Bin\Debug\", "AssemblyName", "AssemblyName")]
        [TestCase(@"Temp\Bin\Debug\", "My.Player.AssemblyName", "My.Player.AssemblyName")]
        [TestCase(@"Temp\Bin\Debug\", "AssemblyName.Player", "AssemblyName.Player")]
        [TestCase(@"Temp\Bin\Debug\Player\", "AssemblyName", "AssemblyName.Player")]
        [TestCase(@"Temp\Bin\Debug\Player\", "AssemblyName.Player", "AssemblyName.Player.Player")]
        public void GetOutputPath_ReturnsPlayerAndeditorOutputPath(string assemblyOutputPath, string assemblyName, string expectedAssemblyName)
        {
            Assert.AreEqual(expectedAssemblyName, m_AssemblyNameProvider.GetProjectName(assemblyOutputPath, assemblyName));
        }

        [Test]
        public void AllEditorAssemblies_AreCollected()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly editorAssembly in editorAssemblies)
            {
                Assert.IsTrue(collectedAssemblies.Any(assembly => assembly.name == editorAssembly.name && assembly.outputPath == @"Temp\Bin\Debug\"), $"{editorAssembly.name}: was not found in collection.");
            }
        }

        [Test]
        public void EditorAssemblies_WillIncludeEditorSettingsDefines()
        {
            var defines = new[] { "DEBUG", "TRACE" }.Concat(EditorUserBuildSettings.activeScriptCompilationDefines);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly editorAssembly in collectedAssemblies)
            {
                CollectionAssert.IsSubsetOf(defines, editorAssembly.defines);
            }
        }

        [Test]
        public void AllEditorAssemblies_HaveAReferenceToUnityEditorAndUnityEngine()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            foreach (Assembly editorAssembly in editorAssemblies)
            {
                Assert.IsTrue(editorAssembly.allReferences.Any(reference => reference.EndsWith("UnityEngine.dll")));
                Assert.IsTrue(editorAssembly.allReferences.Any(reference => reference.EndsWith("UnityEditor.dll")));
            }
        }

        [Test]
        public void PlayerAssemblies_AreNotCollected_BeforeToggling()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsFalse(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name && assembly.outputPath == @"Temp\Bin\Debug\Player\"), $"{playerAssembly.name}: was found in collection.");
            }
        }

        [Test]
        public void AllPlayerAssemblies_AreCollected_AfterToggling()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.PlayerAssemblies);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsTrue(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name && assembly.outputPath == @"Temp\Bin\Debug\Player\"), $"{playerAssembly.name}: was not found in collection.");
            }
        }

        [Test]
        public void AllPlayerAssemblies_HaveAReferenceToUnityEngine()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsTrue(playerAssembly.allReferences.Any(reference => reference.EndsWith("UnityEngine.dll")));
                Assert.IsFalse(playerAssembly.allReferences.Any(reference => reference.EndsWith("UnityEditor.dll")));
            }
        }
    }
}
