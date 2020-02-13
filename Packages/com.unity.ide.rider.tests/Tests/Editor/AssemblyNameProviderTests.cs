using System.Linq;
using NUnit.Framework;
using Packages.Rider.Editor.ProjectGeneration;
using UnityEditor.Compilation;

namespace Packages.Rider.Editor.Tests
{
    public class AssemblyNameProviderTests
    {
        AssemblyNameProvider m_AssemblyNameProvider;

        [SetUp]
        public void SetUp()
        {
            m_AssemblyNameProvider = new AssemblyNameProvider();
        }

        [Test]
        public void AllEditorAssemblies_AreCollected()
        {
            var editorAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly editorAssembly in editorAssemblies)
            {
                Assert.IsTrue(collectedAssemblies.Any(assembly => assembly.name == editorAssembly.name && assembly.outputPath == editorAssembly.outputPath), $"{editorAssembly.name}: was not found in collection.");
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

            if (m_AssemblyNameProvider.ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.PlayerAssemblies))
                m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.PlayerAssemblies);

            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsFalse(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name + ".Player" && assembly.outputPath == playerAssembly.outputPath), $"{playerAssembly.name}: was found in collection.");
            }
        }

        [Test]
        public void AllPlayerAssemblies_AreCollected_AfterToggling()
        {
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            if (!m_AssemblyNameProvider.ProjectGenerationFlag.HasFlag(ProjectGenerationFlag.PlayerAssemblies))
                m_AssemblyNameProvider.ToggleProjectGeneration(ProjectGenerationFlag.PlayerAssemblies);
            var collectedAssemblies = m_AssemblyNameProvider.GetAssemblies(s => true).ToList();

            foreach (Assembly playerAssembly in playerAssemblies)
            {
                Assert.IsTrue(collectedAssemblies.Any(assembly => assembly.name == playerAssembly.name + ".Player" && assembly.outputPath == playerAssembly.outputPath), $"{playerAssembly.name}: was not found in collection.");
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
